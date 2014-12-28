namespace NServiceBus
{
    using System;
    using NServiceBus.Config;
    using NServiceBus.FirstLevelRetries;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    class FirstLevelRetriesBehavior : IBehavior<IncomingContext>
    {
        readonly FlrStatusStorage storage;
        readonly int maxRetries;
        readonly BusNotifications notifications;

        public FirstLevelRetriesBehavior(FlrStatusStorage storage, int maxRetries, BusNotifications notifications)
        {
            this.storage = storage;
            this.maxRetries = maxRetries;
            this.notifications = notifications;
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
                var messageId = context.Get<DequeueSettings>().QueueName + "/" + context.PhysicalMessage.Id;

                var numberOfRetries = storage.GetRetriesForMessage(messageId);

                if (numberOfRetries >= maxRetries)
                {
                    storage.ClearFailuresForMessage(messageId);
                    context.PhysicalMessage.Headers[Headers.FLRetries] = numberOfRetries.ToString();
                    throw;
                }

                storage.IncrementFailuresForMessage(messageId, ex);

                //question: should we invoke this the first time around? feels like the naming is off?
                notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfRetries,context.PhysicalMessage,ex);

                context.AbortReceiveOperation();
            }

        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("FirstLevelRetries", typeof(FirstLevelRetriesBehavior), "Performs first level retries")
            {
                InsertAfter("ReceiveMessage");
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");

                ContainerRegistration((builder, settings) =>
                {
                    var transportConfig = settings.GetConfigSection<TransportConfig>();
                    var maxRetries = transportConfig != null ? transportConfig.MaxRetries : 5;
           
                    return new FirstLevelRetriesBehavior(builder.Build<FlrStatusStorage>(), maxRetries, builder.Build<BusNotifications>());
                });
            }
        }

    }
}