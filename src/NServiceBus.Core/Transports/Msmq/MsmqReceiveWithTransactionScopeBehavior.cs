namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Diagnostics;
    using System.Messaging;
    using System.Transactions;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class MsmqReceiveWithTransactionScopeBehavior : IBehavior<IncomingContext>
    {
        public Address ErrorQueue { get; set; }

        public IsolationLevel IsolationLevel { get; set; }

        public TimeSpan TransactionTimeout { get; set; }


        public void Invoke(IncomingContext context, Action next)
        {
            var queue = context.Get<MessageQueue>();

            //todo: inject this instead
            transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel,
                Timeout = TransactionTimeout
            };

            using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
            {
                Message message;

                if (!TryReceiveMessage(() => queue.Receive(receiveTimeout, MessageQueueTransactionType.Automatic), out message))
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
                    using (var errorQueue = new MessageQueue(NServiceBus.MsmqUtilities.GetFullPath(ErrorQueue), false, true, QueueAccessMode.Send))
                    {
                        errorQueue.Send(message, MessageQueueTransactionType.Automatic);
                    }
                    scope.Complete();
                    return;
                }

                context.Set(IncomingContext.IncomingPhysicalMessageKey, transportMessage);

                next();

                bool messageHandledSuccessfully;
                if (!context.TryGet("TransportReceiver.MessageHandledSuccessfully", out messageHandledSuccessfully))
                {
                    messageHandledSuccessfully = true;
                }

                if (messageHandledSuccessfully)
                {
                    scope.Complete();
                }
            }

        }

        void LogCorruptedMessage(Message message, Exception ex)
        {
            var error = string.Format("Message '{0}' is corrupt and will be moved to '{1}'", message.Id, ErrorQueue.Queue);
            Logger.Error(error, ex);
        }

        [DebuggerNonUserCode]
        bool TryReceiveMessage(Func<Message> receive, out Message message)
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
            //finally
            //{
            //    //peekResetEvent.Set();
            //}

            return false;
        }

        static ILog Logger = LogManager.GetLogger<MsmqReceiveWithTransactionScopeBehavior>();

        TransactionOptions transactionOptions;
        TimeSpan receiveTimeout = TimeSpan.FromSeconds(1);


        public class MsmqReceiveWithTransactionScopeBehaviorRegistration : RegisterStep
        {
            public MsmqReceiveWithTransactionScopeBehaviorRegistration()
                : base("ReceiveMessage", typeof(MsmqReceiveWithTransactionScopeBehavior), "Invokes the decryption logic")
            {
                InsertBefore("HandlerTransactionScopeWrapperBehavior");
            }
        }
    }
}