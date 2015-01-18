namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using Core.Tests.Fakes;
    using NServiceBus.Transports;
    using NUnit.Framework;
    
    [TestFixture]
    class When_subscribing_to_messages : using_the_unicastBus
    {
        readonly Address addressToOwnerOfTestMessage = new Address("TestMessageOwner", "localhost");
        /// <summary>
        /// Set Up
        /// </summary>
        [SetUp]
        public new void SetUp()
        {
            router.RegisterMessageRoute(typeof(TestMessage), addressToOwnerOfTestMessage);
        }

        [Test]
        public void Should_send_the_assemblyQualified_name_as_subscription_type()
        {
            bus.Subscribe<TestMessage>();

            AssertSubscription(m => m.Headers.ContainsKey(Headers.SubscriptionMessageType) &&
                                    m.Headers[Headers.SubscriptionMessageType] == typeof(TestMessage).AssemblyQualifiedName,
                                addressToOwnerOfTestMessage);

        }

        [Test]
        public void Should_set_the_message_intent_to_subscribe()
        {
            bus.Subscribe<TestMessage>();


            AssertSubscription(m => m.MessageIntent == MessageIntentEnum.Subscribe &&  
                                    m.Headers.ContainsKey(Headers.NServiceBusVersion) &&
                                    m.Headers.ContainsKey(Headers.TimeSent)
                                    ,addressToOwnerOfTestMessage);
        }
    }
    
    [TestFixture]
    class When_using_a_non_centralized_pub_sub_transport : using_the_unicastBus
    {
        [Test]
        public void Should_throw_when_subscribing_to_a_message_that_has_no_configured_address()
        {
            Assert.Throws<InvalidOperationException>(() => bus.Subscribe<EventMessage>());
        }

        [Test]
        public void Should_throw_when_unsubscribing_to_a_message_that_has_no_configured_address()
        {
            Assert.Throws<InvalidOperationException>(() => bus.Unsubscribe<EventMessage>());
        }
    }

    [TestFixture]
    class When_using_a_centralized_pub_sub_transport : using_the_unicastBus
    {
        protected override TransportDefinition CreateTransportDefinition()
        {
            return new FakeCentralizedPubSubTransportDefinition();
        }

        [Test]
        public void Should_not_throw_when_subscribing_to_a_message_that_has_no_configured_address()
        {
            Assert.DoesNotThrow(() => bus.Subscribe<EventMessage>());
        }

        [Test]
        public void Should_not_throw_When_unsubscribing_to_a_message_that_has_no_configured_address()
        {
            Assert.DoesNotThrow(() => bus.Unsubscribe<EventMessage>());
        }
    }
}
