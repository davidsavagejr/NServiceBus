namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using AutomaticSubscriptions;
    using Config;
    using Logging;
    using NServiceBus.Hosting;
    using NServiceBus.Settings.Concurrency;
    using NServiceBus.Settings.Throttling;
    using NServiceBus.Support;
    using NServiceBus.Unicast;
    using Pipeline;
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
                s.SetDefault<IConcurrencyConfig>(new SharedConcurrencyConfig(null));
                s.SetDefault<IThrottlingConfig>(new NoLimitThrottlingConfig());
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var hostInfo = new HostInformation(context.Settings.Get<Guid>("NServiceBus.HostInformation.HostId"),
                context.Settings.Get<string>("NServiceBus.HostInformation.DisplayName"),
                context.Settings.Get<Dictionary<string, string>>("NServiceBus.HostInformation.Properties"));

            context.Container.RegisterSingleton(hostInfo);

           

            var concurrencyConfig = context.Settings.Get<IConcurrencyConfig>();
            var throttlingConfig = context.Settings.Get<IThrottlingConfig>();

            var transportConfig = context.Settings.GetConfigSection<TransportConfig>();

            if (transportConfig != null)
            {
                if (transportConfig.MaximumConcurrencyLevel != 0)
                {
                    concurrencyConfig = new SharedConcurrencyConfig(transportConfig.MaximumConcurrencyLevel);
                }
                if (transportConfig.MaximumMessageThroughputPerSecond == 0)
                {
                    throttlingConfig = new NoLimitThrottlingConfig();
                }
                else if (transportConfig.MaximumMessageThroughputPerSecond != -1)
                {
                    throttlingConfig = new SharedLimitThrottlingConfig(transportConfig.MaximumConcurrencyLevel);
                }
            }

            var executor = throttlingConfig.WrapExecutor(concurrencyConfig.BuildExecutor());

            context.Container.ConfigureComponent<MainPipelineFactory>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<SatellitePipelineFactory>(DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent<Unicast.UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(u => u.HostInformation, hostInfo)
                .ConfigureProperty(u => u.Executor, executor);

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
           
            context.Pipeline.Register<InvokeFaultManagerBehavior.Registration>();
            context.Pipeline.Register<EnforceMessageIdBehavior.Registration>();   
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