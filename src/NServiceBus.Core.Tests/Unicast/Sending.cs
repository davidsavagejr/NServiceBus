namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;

    [TestFixture]
    class When_sending_any_message : using_the_unicastBus
    {
        [Test]
        public void Should_generate_a_conversation_id()
        {
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers.ContainsKey(Headers.ConversationId)), Arg<SendOptions>.Is.Anything));
        }

        [Test]
        public void Should_not_override_a_conversation_id_specified_by_the_user()
        {
            RegisterMessageType<TestMessage>();


            bus.Send<TestMessage>(m => bus.SetMessageHeader(m, Headers.ConversationId, "my order id"));

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers[Headers.ConversationId] == "my order id"), Arg<SendOptions>.Is.Anything));
        }

       
    }

    [TestFixture]
    class When_sending_multiple_messages_in_one_go : using_the_unicastBus
    {

        [Test]
        public void Should_be_persistent_if_any_of_the_messages_is_persistent()
        {
            RegisterMessageType<NonPersistentMessage>(configure.LocalAddress);
            RegisterMessageType<PersistentMessage>(configure.LocalAddress);
            bus.Send(new NonPersistentMessage());
            bus.Send(new PersistentMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<SendOptions>.Is.Anything));
        }


        [Test]
        public void Should_use_the_lowest_time_to_be_received()
        {
            RegisterMessageType<NonPersistentMessage>(configure.LocalAddress);
            RegisterMessageType<PersistentMessage>(configure.LocalAddress);
            bus.Send(new NonPersistentMessage());
            bus.Send(new PersistentMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.TimeToBeReceived == TimeSpan.FromMinutes(45)), Arg<SendOptions>.Is.Anything));
        }

        [TimeToBeReceived("00:45:00")]
        class PersistentMessage { }

        [Express]
        class NonPersistentMessage { }
    }



    [TestFixture]
    class When_sending_any_message_from_a_volatile_endpoint : using_the_unicastBus
    {
        [Test]
        public void It_should_be_non_persistent_by_default()
        {
            MessageMetadataRegistry.DefaultToNonPersistentMessages = true;
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => !m.Recoverable), Arg<SendOptions>.Is.Anything));
        }
    }

    [TestFixture]
    class When_sending_a_command_message : using_the_unicastBus
    {
        [Test]
        public void Should_specify_the_message_to_be_recoverable()
        {
            RegisterMessageType<CommandMessage>();

            bus.Send(new CommandMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<SendOptions>.Is.Anything));
        }
    }

}
