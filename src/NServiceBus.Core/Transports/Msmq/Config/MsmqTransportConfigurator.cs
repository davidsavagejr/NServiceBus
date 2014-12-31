namespace NServiceBus.Features
{
    using System;
    using Config;
    using Logging;
    using NServiceBus.Pipeline;
    using Transports;
    using Transports.Msmq;
    using Transports.Msmq.Config;

    /// <summary>
    /// Used to configure the MSMQ transport.
    /// </summary>
    public class MsmqTransportConfigurator : ConfigureTransport
    {
        internal MsmqTransportConfigurator()
        {
            DependsOn<UnicastBus>();
        }

        /// <summary>
        /// Creates a <see cref="RegisterStep"/> for receive behavior.
        /// </summary>
        /// <returns></returns>
        protected override RegisterStep GetReceiveBehaviorRegistration(ReceiveOptions receiveOptions)
        {
            if (receiveOptions.Transactions.SuppressDistributedTransactions)
            {
                throw new NotImplementedException();
            }
            else
            {
                return new MsmqReceiveWithTransactionScopeBehavior.Registration(receiveOptions);
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigureTransport"/>.
        /// </summary>
        protected override void Configure(FeatureConfigurationContext context, string connectionString)
        {
            new CheckMachineNameForComplianceWithDtcLimitation()
            .Check();

            context.Container.ConfigureComponent<CorrelationIdMutatorForBackwardsCompatibilityWithV3>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<MsmqUnitOfWork>(DependencyLifecycle.SingleInstance);

            var endpointIsTransactional = context.Settings.Get<bool>("Transactions.Enabled");
            var doNotUseDTCTransactions = context.Settings.Get<bool>("Transactions.SuppressDistributedTransactions");


            if (!context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                context.Container.ConfigureComponent(b => new MsmqDequeueStrategy(b.Build<CriticalError>(), endpointIsTransactional),
                    DependencyLifecycle.InstancePerCall);
            }

            var cfg = context.Settings.GetConfigSection<MsmqMessageQueueConfig>();

            var settings = new MsmqSettings();
            if (cfg != null)
            {
                settings.UseJournalQueue = cfg.UseJournalQueue;
                settings.UseDeadLetterQueue = cfg.UseDeadLetterQueue;

                Logger.Warn(Message);
            }
            else
            {
                if (connectionString != null)
                {
                    settings = new MsmqConnectionStringBuilder(connectionString).RetrieveSettings();
                }
            }

            context.Container.ConfigureComponent<MsmqMessageSender>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings)
                .ConfigureProperty(t => t.SuppressDistributedTransactions, doNotUseDTCTransactions);

            context.Container.ConfigureComponent<MsmqQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Settings, settings);
        }

        /// <summary>
        /// <see cref="ConfigureTransport.ExampleConnectionStringForErrorMessage"/>
        /// </summary>
        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "cacheSendConnection=true;journal=false;deadLetter=true"; }
        }

        /// <summary>
        /// <see cref="ConfigureTransport.RequiresConnectionString"/>
        /// </summary>
        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        static ILog Logger = LogManager.GetLogger<MsmqTransportConfigurator>();

        const string Message =
            @"
MsmqMessageQueueConfig section has been deprecated in favor of using a connectionString instead.
Here is an example of what is required:
  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""cacheSendConnection=true;journal=false;deadLetter=true"" />
  </connectionStrings>";



    }

}