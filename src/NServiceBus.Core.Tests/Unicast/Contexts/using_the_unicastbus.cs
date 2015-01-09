namespace NServiceBus.Unicast.Tests.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;
    using System.Threading;
    using Core.Tests;
    using Helpers;
    using Licensing;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using MessageMutator;
    using Monitoring;
    using NServiceBus.Core.Tests.Fakes;
    using NServiceBus.Hosting;
    using NServiceBus.ObjectBuilder;
    using NUnit.Framework;
    using Pipeline;
    using Publishing;
    using Rhino.Mocks;
    using Routing;
    using Serialization;
    using Serializers.XML;
    using Settings;
    using Subscriptions.MessageDrivenSubscriptions;
    using Timeout;
    using Transports;
    using Unicast.Messages;
    using UnitOfWork;

    class using_a_configured_unicastBus
    {
        protected UnicastBus bus;

        protected ISendMessages messageSender;
        protected FakeSubscriptionStorage subscriptionStorage;

        protected MessageMapper MessageMapper = new MessageMapper();

        //protected FakeTransport Transport;
        protected XmlMessageSerializer MessageSerializer;
        protected FuncBuilder Builder;
        public static Address MasterNodeAddress;
        protected EstimatedTimeToSLABreachCalculator SLABreachCalculator = (EstimatedTimeToSLABreachCalculator) FormatterServices.GetUninitializedObject(typeof(EstimatedTimeToSLABreachCalculator));
        protected MessageMetadataRegistry MessageMetadataRegistry;
        protected SubscriptionManager subscriptionManager;
        protected StaticMessageRouter router;

        protected MessageHandlerRegistry handlerRegistry;
        protected TransportDefinition transportDefinition;
        protected SettingsHolder settings;
        protected Configure configure;
        protected PipelineModifications pipelineModifications;

        PipelineExecutor pipelineFactory;
        protected FakeMessagePump MessagePump;

        static using_a_configured_unicastBus()
        {
            var localAddress = "endpointA";
            MasterNodeAddress = new Address(localAddress, "MasterNode");
        }

        [SetUp]
        public void SetUp()
        {
            LicenseManager.InitializeLicense();
            transportDefinition = CreateTransportDefinition();
            
            settings = new SettingsHolder();

            settings.SetDefault("EndpointName", "TestEndpoint");
            settings.SetDefault("Endpoint.SendOnly", false);
            settings.SetDefault("MasterNode.Address", MasterNodeAddress);
            settings.SetDefault("NServiceBus.LocalAddress", "TestEndpoint");
            OverrideSettings(settings);

            pipelineModifications = new PipelineModifications();
            settings.Set<PipelineModifications>(pipelineModifications);

            ApplyPipelineModifications();

            Builder = new FuncBuilder();

            Builder.Register<ReadOnlySettings>(() => settings);

            router = new StaticMessageRouter(KnownMessageTypes());
            var conventions = new Conventions();
            handlerRegistry = new MessageHandlerRegistry(conventions);
            MessageMetadataRegistry = new MessageMetadataRegistry(false, conventions);
            MessageSerializer = new XmlMessageSerializer(MessageMapper, conventions);

            messageSender = MockRepository.GenerateStub<ISendMessages>();
            subscriptionStorage = new FakeSubscriptionStorage();
            var localAddress = Address.Parse("TestEndpoint");
            configure = new Configure(settings, Builder, new List<Action<IConfigureComponents>>(), new PipelineSettings(new PipelineModifications()))
            {
                localAddress = localAddress
            };

            subscriptionManager = new SubscriptionManager
                {
                    MessageSender = messageSender,
                    SubscriptionStorage = subscriptionStorage,
                    Configure = configure
                };

            var behaviorContextStacker = new BehaviorContextStacker(Builder);
            Builder.Register<BehaviorContextStacker>(() => behaviorContextStacker);

            var outgoingMessageHeaders = new StaticOutgoingMessageHeaders();
            Builder.Register<StaticOutgoingMessageHeaders>(() => outgoingMessageHeaders);

            var callbackMessageLookup = new CallbackMessageLookup();
            Builder.Register<CallbackMessageLookup>(() => callbackMessageLookup);

            Builder.Register<CallbackInvocationBehavior>(() => new CallbackInvocationBehavior(callbackMessageLookup));

            var pipelineSettings = new PipelineSettings(pipelineModifications);
            HardcodedPipelineSteps.Register(pipelineSettings, false);

            var receiveBehaviorRegistration = new ReceiveBehaviorRegistration();
            receiveBehaviorRegistration.ContainerRegistration((b, s) => new FakeReceiveBehavior());
            pipelineSettings.Register(receiveBehaviorRegistration);

            pipelineFactory = new PipelineExecutor(settings, Builder, new BusNotifications());

            Builder.Register<IMessageSerializer>(() => MessageSerializer);
            Builder.Register<ISendMessages>(() => messageSender);

            Builder.Register<LogicalMessageFactory>(() => new LogicalMessageFactory(MessageMetadataRegistry, MessageMapper, behaviorContextStacker.GetCurrentContext()));

            Builder.Register<IManageSubscriptions>(() => subscriptionManager);
            Builder.Register<EstimatedTimeToSLABreachCalculator>(() => SLABreachCalculator);
            Builder.Register<MessageMetadataRegistry>(() => MessageMetadataRegistry);

            Builder.Register<IMessageHandlerRegistry>(() => handlerRegistry);
            Builder.Register<IMessageMapper>(() => MessageMapper);

            Builder.Register<ReceiveBehavior>(() => new FakeReceiveBehavior());
            Builder.Register<DeserializeLogicalMessagesBehavior>(() => new DeserializeLogicalMessagesBehavior
                                                             {
                                                                 MessageSerializer = MessageSerializer,
                                                                 MessageMetadataRegistry = MessageMetadataRegistry,
                                                             });

            Builder.Register<CreatePhysicalMessageBehavior>(() => new CreatePhysicalMessageBehavior());
            Builder.Register<PipelineExecutor>(() => pipelineFactory);
            Builder.Register<TransportDefinition>(() => transportDefinition);
            MessagePump = new FakeMessagePump();
            Builder.Register<IDequeueMessages>(() => MessagePump);

            var deferrer = new TimeoutManagerDeferrer
            {
                MessageSender = messageSender,
                TimeoutManagerAddress = MasterNodeAddress.SubScope("Timeouts"),
                Configure = configure,
            };

            Builder.Register<IDeferMessages>(() => deferrer);
            Builder.Register<IPublishMessages>(() => new StorageDrivenPublisher(subscriptionStorage, messageSender, null, behaviorContextStacker.GetCurrentContext()));

            bus = new UnicastBus(
                new FakeExecutor(), 
                null,
                 new PipelineFactory[] {new MainPipelineFactory(), new SatellitePipelineFactory()},
                 new MessageMapper(), 
                 Builder,
                 configure,
                 subscriptionManager,
                 MessageMetadataRegistry,
                 settings,
                 transportDefinition,
                 messageSender,
                 router, 
                 outgoingMessageHeaders,
                 callbackMessageLookup,
                 pipelineFactory
                )
            {
                HostInformation = new HostInformation(Guid.NewGuid(), "HelloWorld")
            };

            Builder.Register<IMutateOutgoingTransportMessages>(() => new CausationMutator { Bus = bus });
            Builder.Register<IBus>(() => bus);
            Builder.Register<UnicastBus>(() => bus);
            Builder.Register<Conventions>(() => conventions);
            Builder.Register<Configure>(() => configure);
        }

        protected virtual TransportDefinition CreateTransportDefinition()
        {
            return new MsmqTransport();
        }

        protected virtual void OverrideSettings(SettingsHolder settings)
        {
        }

        protected virtual void ApplyPipelineModifications()
        {
        }

        protected virtual IEnumerable<Type> KnownMessageTypes()
        {
            return new Collection<Type>();
        }

        protected void VerifyThatMessageWasSentTo(Address destination)
        {
            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<SendOptions>.Matches(o => o.Destination == destination)));
        }

        protected void VerifyThatMessageWasSentWithHeaders(Func<IDictionary<string, string>, bool> predicate)
        {
            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(t => predicate(t.Headers)), Arg<SendOptions>.Is.Anything));
        }

        protected void RegisterUow(IManageUnitsOfWork uow)
        {
            Builder.Register<IManageUnitsOfWork>(() => uow);
        }

        protected void RegisterMessageHandlerType<T>() where T : new()
        {
// ReSharper disable HeapView.SlowDelegateCreation
            Builder.Register<T>(() => new T());
// ReSharper restore HeapView.SlowDelegateCreation

            handlerRegistry.RegisterHandler(typeof(T));
        }
        protected void RegisterOwnedMessageType<T>()
        {
            router.RegisterMessageRoute(typeof(T), configure.LocalAddress);
        }
        protected Address RegisterMessageType<T>()
        {
            var address = new Address(typeof(T).Name, "localhost");
            RegisterMessageType<T>(address);

            return address;
        }

        protected void RegisterMessageType<T>(Address address)
        {
            MessageMapper.Initialize(new[] { typeof(T) });
            MessageSerializer.Initialize(new[] { typeof(T) });
            router.RegisterMessageRoute(typeof(T), address);
            MessageMetadataRegistry.RegisterMessageType(typeof(T));

        }

        protected void StartBus()
        {
            ((IStartableBus)bus).Start();
        }

        protected void AssertSubscription(Predicate<TransportMessage> condition, Address addressOfPublishingEndpoint)
        {
            try
            {
                messageSender.AssertWasCalled(x =>
                  x.Send(Arg<TransportMessage>.Matches(m => condition(m)), Arg<SendOptions>.Matches(o => o.Destination == addressOfPublishingEndpoint)));

            }
            catch (Exception)
            {
                //retry to avoid race conditions 
                Thread.Sleep(2000);
                messageSender.AssertWasCalled(x =>
                 x.Send(Arg<TransportMessage>.Matches(m => condition(m)), Arg<SendOptions>.Matches(o => o.Destination == addressOfPublishingEndpoint)));
            }
        }

        protected void AssertSubscription<T>(Address addressOfPublishingEndpoint)
        {
            try
            {
                messageSender.AssertWasCalled(x =>
                  x.Send(Arg<TransportMessage>.Matches(m => IsSubscriptionFor<T>(m)), Arg<SendOptions>.Matches(o => o.Destination == addressOfPublishingEndpoint)));

            }
            catch (Exception)
            {
                //retry to avoid race conditions 
                Thread.Sleep(1000);
                messageSender.AssertWasCalled(x =>
                  x.Send(Arg<TransportMessage>.Matches(m => IsSubscriptionFor<T>(m)), Arg<SendOptions>.Matches(o => o.Destination == addressOfPublishingEndpoint)));
            }
        }

        bool IsSubscriptionFor<T>(TransportMessage transportMessage)
        {
            var type = Type.GetType(transportMessage.Headers[Headers.SubscriptionMessageType]);

            return type == typeof(T);
        }
    }


    class using_the_unicastBus : using_a_configured_unicastBus
    {
        [SetUp]
        public new void SetUp()
        {
            StartBus();
        }

        protected Exception ResultingException;

        protected void ReceiveMessage(TransportMessage transportMessage)
        {
            try
            {
                //bus.GetHeaderAction = (o, s) =>
                //{
                //    string v;
                //    transportMessage.Headers.TryGetValue(s, out v);
                //    return v;
                //};

                //bus.SetHeaderAction = (o, s, v) => { transportMessage.Headers[s] = v; };
                MessagePump.SignalMessageAvailable(transportMessage);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Fake message processing failed: " + ex);
                ResultingException = ex;
            }
        }

        protected void ReceiveMessage<T>(T message, IDictionary<string, string> headers = null, MessageMapper mapper = null)
        {
            RegisterMessageType<T>();
            var messageToReceive = Helpers.Serialize(message, mapper: mapper);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    messageToReceive.Headers[header.Key] = header.Value;
                }
            }

            ReceiveMessage(messageToReceive);
        }

        protected void SimulateMessageBeingAbortedDueToRetryCountExceeded(TransportMessage transportMessage)
        {
            try
            {
                //Transport.FakeMessageBeingPassedToTheFaultManager(transportMessage);
            }
            catch (Exception ex)
            {
                ResultingException = ex;
            }
        }
    }
}
