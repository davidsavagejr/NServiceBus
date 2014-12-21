namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using NServiceBus.Faults;
    using NServiceBus.Hosting;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    class FirstLevelRetriesBehavior : IBehavior<IncomingContext>
    {
        public FirstLevelRetriesBehavior(IManageMessageFailures manageMessageFailures, bool isTransactional, HostInformation hostInformation)
        {
            FailureManager = manageMessageFailures;
            this.isTransactional = isTransactional;
            this.hostInformation = hostInformation;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            firstLevelRetries = context.Get<FirstLevelRetries>();
            ProcessMessage(context.PhysicalMessage, next);
        }

        bool ShouldExitBecauseOfRetries(TransportMessage message)
        {
            if (isTransactional)
            {
                if (firstLevelRetries.HasMaxRetriesForMessageBeenReached(message))
                {
                    return true;
                }
            }
            return false;
        }

        void ProcessMessage(TransportMessage message, Action next)
        {
            message.Headers[Headers.HostId] = hostInformation.HostId.ToString("N");
            message.Headers[Headers.HostDisplayName] = hostInformation.DisplayName;

            if (string.IsNullOrWhiteSpace(message.Id))
            {
                Logger.Error("Message without message id detected");

                FailureManager.SerializationFailedForMessage(message,
                    new SerializationException("Message without message id received."));

                return;
            }

            if (ShouldExitBecauseOfRetries(message))
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
        readonly bool isTransactional;
        readonly HostInformation hostInformation;
        FirstLevelRetries firstLevelRetries;
        ILog Logger = LogManager.GetLogger<FirstLevelRetriesBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("FirstLevelRetriesBehavior", typeof(FirstLevelRetriesBehavior), "Performs first level retries")
            {
                InsertBefore("ReceivePerformanceDiagnosticsBehavior");

                ContainerRegistration((builder, settings) =>
                {
                    var hostInfo = new HostInformation(settings.Get<Guid>("NServiceBus.HostInformation.HostId"),
               settings.Get<string>("NServiceBus.HostInformation.DisplayName"),
               settings.Get<Dictionary<string, string>>("NServiceBus.HostInformation.Properties"));


                    return new FirstLevelRetriesBehavior(builder.Build<IManageMessageFailures>(), settings.Get<bool>("Transactions.Enabled"), hostInfo);
                });
            }
        }

    }
}