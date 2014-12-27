namespace NServiceBus
{
    using System;
    using NServiceBus.Faults;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class InvokeFaultManagerBehavior : IBehavior<IncomingContext>
    {
        public InvokeFaultManagerBehavior(IManageMessageFailures failureManager)
        {
            this.failureManager = failureManager;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            var message = context.PhysicalMessage;
           
            try
            {
                next();
            }
            catch (MessageDeserializationException serializationException)
            {
                Logger.Error("Failed to deserialize message with ID: " + message.Id, serializationException);

                message.RevertToOriginalBodyIfNeeded();

                failureManager.SerializationFailedForMessage(message, serializationException);
            }

            catch (Exception exception)
            {
                Logger.Error("Failed to process message with ID: " + message.Id, exception);

                message.RevertToOriginalBodyIfNeeded();

                failureManager.ProcessingAlwaysFailsForMessage(message, exception);
            }
        }

        readonly IManageMessageFailures failureManager;
        ILog Logger = LogManager.GetLogger<InvokeFaultManagerBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("InvokeFaultManager", typeof(InvokeFaultManagerBehavior), "Invokes the configured fault manager for messages that fails processing (and any retries)")
            {
                InsertAfter("ReceiveMessage");

                InsertBeforeIfExists("FirstLevelRetries");
            }
        }
    }
}