namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using AutomaticSubscriptions;
    using Config;
    using Faults;
    using Logging;
    using NServiceBus.Hosting;
    using NServiceBus.Support;
    using NServiceBus.Unicast;
    using Pipeline;
    using Transports;
    using Unicast.Messages;
    using Unicast.Routing;
    using Unicast.Transport;

    class UnicastBus : Feature
    {
        internal UnicastBus()
        {
            EnableByDefault();

            Defaults(s =>
            {
                string fullPathToStartingExe;
                s.SetDefault("NServiceBus.HostInformation.HostId", GenerateDefaultHostId(out fullPathToStartingExe));
                s.SetDefault("NServiceBus.HostInformation.DisplayName", RuntimeEnvironment.MachineName);
                s.SetDefault("NServiceBus.HostInformation.Properties", new Dictionary<string, string>
                {
                    {"Machine", RuntimeEnvironment.MachineName},
                    {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
                    {"UserName", Environment.UserName},
                    {"PathToExecutable", fullPathToStartingExe}
                });
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var hostInfo = new HostInformation(context.Settings.Get<Guid>("NServiceBus.HostInformation.HostId"),
                context.Settings.Get<string>("NServiceBus.HostInformation.DisplayName"),
                context.Settings.Get<Dictionary<string, string>>("NServiceBus.HostInformation.Properties"));

            context.Container.RegisterSingleton(hostInfo);
            
            var defaultAddress = context.Settings.LocalAddress();

            

            var transportConfig = context.Settings.GetConfigSection<TransportConfig>();
            var maximumConcurrencyLevel = 1;

            if (transportConfig != null)
            {
                maximumConcurrencyLevel = transportConfig.MaximumConcurrencyLevel;
            }


            context.Container.ConfigureComponent<Unicast.UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(u => u.InputAddress, defaultAddress)
                .ConfigureProperty(u => u.HostInformation, hostInfo)
                .ConfigureProperty(u => u.MaximumConcurrencyLevel, maximumConcurrencyLevel)
                .ConfigureProperty(u => u.PurgeOnStartup, context.Settings.GetOrDefault<bool>("Transport.PurgeOnStartup"));

            ConfigureSubscriptionAuthorization(context);

            context.Container.ConfigureComponent<PipelineExecutor>(DependencyLifecycle.SingleInstance);

            var knownMessages = context.Settings.GetAvailableTypes()
                .Where(context.Settings.Get<Conventions>().IsMessageType)
                .ToList();

            RegisterMessageOwnersAndBusAddress(context, knownMessages);

            ConfigureMessageRegistry(context, knownMessages);

            if (context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            var transactionSettings = new TransactionSettings(context.Settings);

            if (transactionSettings.DoNotWrapHandlersExecutionInATransactionScope)
            {
                context.Pipeline.Register<SuppressAmbientTransactionBehavior.Registration>();
            }
            else
            {
                context.Pipeline.Register<HandlerTransactionScopeWrapperBehavior.Registration>();
            }
           
            
            if (transactionSettings.IsTransactional)
            {
                context.Container.ConfigureComponent<FlrStatusStorage>(DependencyLifecycle.SingleInstance);
                context.Pipeline.Register<FirstLevelRetriesBehavior.Registration>();
            }
           

            context.Pipeline.Register<InvokeFaultManagerBehavior.Registration>();
            context.Pipeline.Register<EnforceMessageIdBehavior.Registration>();   
  
           
            context.Container.ConfigureComponent(b => new MainTransportReceiver(transactionSettings, b.Build<IDequeueMessages>(), b.Build<IManageMessageFailures>(), context.Settings, b.Build<Configure>(), b.Build<PipelineExecutor>())
            {
                Notifications = b.Build<BusNotifications>()
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b => new SatelliteTransportReceiver(transactionSettings, b.Build<IDequeueMessages>(), b.Build<IManageMessageFailures>(), context.Settings, b.Build<Configure>())
            {
                Notifications = b.Build<BusNotifications>()
            }, DependencyLifecycle.InstancePerCall);
        }

        static Guid GenerateDefaultHostId(out string fullPathToStartingExe)
        {
            var gen = new DefaultHostIdGenerator(Environment.CommandLine, RuntimeEnvironment.MachineName);

            fullPathToStartingExe = gen.FullPathToStartingExe;

            return gen.HostId;
        }



        void ConfigureSubscriptionAuthorization(FeatureConfigurationContext context)
        {
            var authType = context.Settings.GetAvailableTypes().FirstOrDefault(t => typeof(IAuthorizeSubscriptions).IsAssignableFrom(t) && !t.IsInterface);

            if (authType != null)
            {
                context.Container.ConfigureComponent(authType, DependencyLifecycle.SingleInstance);
            }
        }

        void RegisterMessageOwnersAndBusAddress(FeatureConfigurationContext context, IEnumerable<Type> knownMessages)
        {
            var unicastConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            var router = new StaticMessageRouter(knownMessages);
            var key = typeof(AutoSubscriptionStrategy).FullName + ".SubscribePlainMessages";

            if (context.Settings.HasSetting(key))
            {
                router.SubscribeToPlainMessages = context.Settings.Get<bool>(key);
            }

            context.Container.RegisterSingleton(router);

            if (unicastConfig == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(unicastConfig.ForwardReceivedMessagesTo))
            {
                var forwardAddress = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
                context.Container.ConfigureProperty<ForwardBehavior>(b => b.ForwardReceivedMessagesTo, forwardAddress);
            }

            if (unicastConfig.TimeToBeReceivedOnForwardedMessages != TimeSpan.Zero)
            {
                context.Container.ConfigureProperty<ForwardBehavior>(b => b.TimeToBeReceivedOnForwardedMessages, unicastConfig.TimeToBeReceivedOnForwardedMessages);
            }

            var messageEndpointMappings = unicastConfig.MessageEndpointMappings.Cast<MessageEndpointMapping>()
                .OrderByDescending(m => m)
                .ToList();

            foreach (var mapping in messageEndpointMappings)
            {
                mapping.Configure((messageType, address) =>
                {
                    var conventions = context.Settings.Get<Conventions>();
                    if (!(conventions.IsMessageType(messageType) || conventions.IsEventType(messageType) || conventions.IsCommandType(messageType)))
                    {
                        return;
                    }

                    if (conventions.IsEventType(messageType))
                    {
                        router.RegisterEventRoute(messageType, address);
                        return;
                    }

                    router.RegisterMessageRoute(messageType, address);
                });
            }
        }
        void ConfigureMessageRegistry(FeatureConfigurationContext context, IEnumerable<Type> knownMessages)
        {
            var messageRegistry = new MessageMetadataRegistry(!DurableMessagesConfig.GetDurableMessagesEnabled(context.Settings), context.Settings.Get<Conventions>());

            foreach (var msg in knownMessages)
            {
                messageRegistry.RegisterMessageType(msg);
            }

            context.Container.RegisterSingleton(messageRegistry);
            context.Container.ConfigureComponent<LogicalMessageFactory>(DependencyLifecycle.SingleInstance);

            if (!Logger.IsInfoEnabled)
            {
                return;
            }

            var messageDefinitions = messageRegistry.GetAllMessages().ToList();

            Logger.InfoFormat("Number of messages found: {0}", messageDefinitions.Count());

            if (!Logger.IsDebugEnabled)
            {
                return;
            }

            Logger.DebugFormat("Message definitions: \n {0}",
                string.Concat(messageDefinitions.Select(md => md.ToString() + "\n")));
        }

        static ILog Logger = LogManager.GetLogger<UnicastBus>();
    }
}