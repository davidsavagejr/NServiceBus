namespace NServiceBus
{
    using System;
    using NServiceBus.Faults;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    class FirstLevelRetriesBehavior : IBehavior<IncomingContext>
    {
        public FirstLevelRetriesBehavior(IManageMessageFailures manageMessageFailures)
        {
            FailureManager = manageMessageFailures;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            firstLevelRetries = context.Get<FirstLevelRetries>();

            var message = context.PhysicalMessage;

            if (firstLevelRetries.HasMaxRetriesForMessageBeenReached(message))
            {
                return;
            }

            try
            {
                next();
            }
            catch (MessageDeserializationException serializationException)
            {
                Logger.Error("Failed to deserialize message with ID: " + message.Id, serializationException);

                message.RevertToOriginalBodyIfNeeded();

                FailureManager.SerializationFailedForMessage(message, serializationException);
            }
        }


        readonly IManageMessageFailures FailureManager;
        FirstLevelRetries firstLevelRetries;
        ILog Logger = LogManager.GetLogger<FirstLevelRetriesBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("FirstLevelRetriesBehavior", typeof(FirstLevelRetriesBehavior), "Performs first level retries")
            {
                InsertAfter("ReceiveBehavior");
                InsertBefore("ReceivePerformanceDiagnosticsBehavior");
            }
        }

    }
}