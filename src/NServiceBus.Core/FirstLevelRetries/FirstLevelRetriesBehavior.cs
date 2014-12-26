namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    class FirstLevelRetriesBehavior : IBehavior<IncomingContext>
    {
        readonly FlrStatusStorage storage;

        public FirstLevelRetriesBehavior(FlrStatusStorage storage)
        {
            this.storage = storage;
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
            catch (Exception)
            {
                var message = context.PhysicalMessage;

                if (storage.HasMaxRetriesForMessageBeenReached(message))
                {
                    throw;
                }

                //tell the transport to roll the message back to the queue again
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