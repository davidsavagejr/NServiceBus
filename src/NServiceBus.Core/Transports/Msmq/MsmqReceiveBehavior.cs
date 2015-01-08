namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Messaging;
    using System.Threading;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    abstract class MsmqReceiveBehavior:ReceiveBehavior
    {
       
        [DebuggerNonUserCode]
        protected bool TryReceiveMessage(Func<Message> receive, BootstrapContext context, out Message message)
        {
            message = null;
            
            var peekResetEvent = context.Get<AutoResetEvent>("MsmqDequeueStrategy.PeekResetEvent");
             
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
            }
            finally
            {
                peekResetEvent.Set();
            }

            return false;
        }

        protected bool TryConvertMessage(Message message,  BootstrapContext context,out TransportMessage transportMessage)
        {
            try
            {
                transportMessage = MsmqUtilities.Convert(message);

                return true;
            }
            catch (Exception ex)
            {
                var errorQueue = context.Get<Address>("MsmqDequeueStrategy.ErrorQueue");

                LogCorruptedMessage(message, ex, errorQueue);


                using (var nativeErrorQueue = new MessageQueue(MsmqUtilities.GetFullPath(errorQueue), false, true, QueueAccessMode.Send))
                {
                    nativeErrorQueue.Send(message, MessageQueueTransactionType.Automatic);
                }

                transportMessage = null;

                return false;
            }
        }

      

        void LogCorruptedMessage(Message message, Exception ex, Address errorQueue)
        {
            var error = string.Format("Message '{0}' is corrupt and will be moved to '{1}'", message.Id, errorQueue.Queue);
            Logger.Error(error, ex);
        }

        static ILog Logger = LogManager.GetLogger<MsmqReceiveWithTransactionScopeBehavior>();
    }
}