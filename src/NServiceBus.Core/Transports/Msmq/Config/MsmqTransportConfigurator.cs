﻿namespace NServiceBus.Features
{
    using System;
    using Config;
    using Logging;
    using NServiceBus.Faults;
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
        /// Initializes a new instance of <see cref="ConfigureTransport"/>.
        /// </summary>
        protected override void Configure(FeatureConfigurationContext context, string connectionString)
        {
            new CheckMachineNameForComplianceWithDtcLimitation()
            .Check();

            context.Container.ConfigureComponent<CorrelationIdMutatorForBackwardsCompatibilityWithV3>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<MsmqUnitOfWork>(DependencyLifecycle.SingleInstance);


            var doNotUseDTCTransactions = context.Settings.Get<bool>("Transactions.SuppressDistributedTransactions");

            if (!context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                var configuredErrorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);

                context.Container.ConfigureComponent<MsmqDequeueStrategy>(DependencyLifecycle.InstancePerCall);

                if (doNotUseDTCTransactions)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    context.Pipeline.Register<MsmqReceiveWithTransactionScopeBehavior.MsmqReceiveWithTransactionScopeBehaviorRegistration>();
                    context.Container.ConfigureProperty<MsmqReceiveWithTransactionScopeBehavior>(o => o.ErrorQueue, configuredErrorQueue);
                    context.Container.ConfigureProperty<MsmqReceiveWithTransactionScopeBehavior>(o => o.TransactionTimeout, null);
                    context.Container.ConfigureProperty<MsmqReceiveWithTransactionScopeBehavior>(o => o.IsolationLevel, null);
                }
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