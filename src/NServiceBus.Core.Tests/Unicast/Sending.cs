namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;

   

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
}
