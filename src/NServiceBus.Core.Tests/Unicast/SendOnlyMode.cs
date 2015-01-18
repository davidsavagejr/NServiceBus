namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;

   
   
    [TestFixture]
    class When_replying_to_a_message_that_was_sent_with_null_reply_to_address : using_the_unicastBus
    {
        [Test]
        public void Should_blow()
        {
            RegisterMessageType<TestMessage>();
            var receivedMessage = Helpers.Helpers.Serialize(new TestMessage(),true);
            RegisterMessageHandlerType<HandlerThatRepliesWithACommandToAMessage>();
            ReceiveMessage(receivedMessage);
            Assert.IsInstanceOf<InvalidOperationException>(ResultingException.GetBaseException());
        }
    }

    [TestFixture]
    class When_returning_to_a_message_that_was_sent_with_null_reply_to_address : using_the_unicastBus
    {
        [Test]
        public void Should_blow()
        {
            RegisterMessageType<TestMessage>();
            var receivedMessage = Helpers.Helpers.Serialize(new TestMessage(),true);
            RegisterMessageHandlerType<HandlerThatReturns>();
            ReceiveMessage(receivedMessage);
            Assert.IsInstanceOf<InvalidOperationException>(ResultingException.GetBaseException());
        }
    }

    public class TestMessage : IMessage
    {
    }
    
    class HandlerThatRepliesWithACommandToAMessage : IHandleMessages<TestMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(TestMessage message)
        {
            Bus.Reply(new TestMessage());
        }
    }

    class HandlerThatReturns : IHandleMessages<TestMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(TestMessage message)
        {
            Bus.Return(1);
        }
    }

}