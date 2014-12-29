namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Diagnostics;
    using System.Messaging;
    using System.Threading;
    using System.Transactions;
    using NServiceBus.Faults;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class MsmqReceiveWithTransactionScopeBehavior : IBehavior<IncomingContext>
    {
        public MsmqReceiveWithTransactionScopeBehavior(TransactionOptions transactionOptions, Address errorQueue)
        {
            this.transactionOptions = transactionOptions;
            this.errorQueue = errorQueue;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            var queue = context.Get<MessageQueue>();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
            {
                Message message;
                var peekResetEvent = context.Get<AutoResetEvent>("MsmqDequeueStrategy.PeekResetEvent");
                if (!TryReceiveMessage(() => queue.Receive(TimeSpan.FromSeconds(1), MessageQueueTransactionType.Automatic), peekResetEvent, out message))
                {
                    scope.Complete();
                    return;
                }

                TransportMessage transportMessage;
                try
                {
                    transportMessage = NServiceBus.MsmqUtilities.Convert(message);
                }
                catch (Exception ex)
                {
                    LogCorruptedMessage(message, ex);
                    using (var nativeErrorQueue = new MessageQueue(NServiceBus.MsmqUtilities.GetFullPath(errorQueue), false, true, QueueAccessMode.Send))
                    {
                        nativeErrorQueue.Send(message, MessageQueueTransactionType.Automatic);
                    }
                    scope.Complete();
                    return;
                }

                context.Set(IncomingContext.IncomingPhysicalMessageKey, transportMessage);

                next();

                if (context.MessageHandledSuccessfully())
                {
                    scope.Complete();
                }
            }

        }

        void LogCorruptedMessage(Message message, Exception ex)
        {
            var error = string.Format("Message '{0}' is corrupt and will be moved to '{1}'", message.Id, errorQueue.Queue);
            Logger.Error(error, ex);
        }

        [DebuggerNonUserCode]
        bool TryReceiveMessage(Func<Message> receive, AutoResetEvent peekResetEvent, out Message message)
        {
            message = null;

            try
            {
                message = receive();
                return true;
            }
            catch (MessageQueueException messageQueueException)
            {
                if (messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    //We should only get an IOTimeout exception here if another process removed the message between us peeking and now.
                    return false;
                }

                // RaiseCriticalException(messageQueueException);
            }
            //catch (Exception ex)
            //{
            //    //Logger.Error("Error in receiving messages.", ex);
            //}
            finally
            {
                peekResetEvent.Set();
            }

            return false;
        }

        readonly TransactionOptions transactionOptions;
        readonly Address errorQueue;

        static ILog Logger = LogManager.GetLogger<MsmqReceiveWithTransactionScopeBehavior>();

        public class Registration : RegisterStep
        {
            public Registration(): base("ReceiveMessage", typeof(MsmqReceiveWithTransactionScopeBehavior), "Performs a msmq receive using a transaction scope. This will require DTC to be enable on the machine")
            {
                InsertBeforeIfExists(WellKnownStep.ExecuteLogicalMessages);
                ContainerRegistration((builder, settings) =>
                {
                    var transactionOptions = new TransactionOptions
                   {
                       IsolationLevel = settings.Get<IsolationLevel>("Transactions.IsolationLevel"),
                       Timeout = settings.Get<TimeSpan>("Transactions.DefaultTimeout")
                   };

                    var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(settings);

                    return new MsmqReceiveWithTransactionScopeBehavior(transactionOptions, errorQueue);
                });
            }
        }
    }
}