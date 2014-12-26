namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    class FirstLevelRetriesBehavior : IBehavior<IncomingContext>
    {
        readonly FlrStatusStorage storage;
        readonly int maxRetries;

        public FirstLevelRetriesBehavior(FlrStatusStorage storage, int maxRetries)
        {
            this.storage = storage;
            this.maxRetries = maxRetries;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            try
            {
                next();
            }
            catch (MessageDeserializationException)
            {
                throw; // no retries for poison messages
            }
            catch (Exception ex)
            {
                var messageId = context.PhysicalMessage.Id;
                var numberOfRetries = storage.GetRetriesForMessage(messageId);

                if (numberOfRetries >= maxRetries)
                {
                    storage.ClearFailuresForMessage(messageId);
                    context.PhysicalMessage.Headers[Headers.FLRetries] = numberOfRetries.ToString();
                    throw;
                }

                storage.IncrementFailuresForMessage(messageId, ex);
                context.AbortReceiveOperation();
            }

        }

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