namespace NServiceBus.Unicast.Tests
{
    using Contexts;
    using NUnit.Framework;

    [TestFixture]
    class When_receiving_any_message : using_the_unicastBus
    {
      
        [Test]
        public void Should_throw_when_there_are_no_registered_message_handlers()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());
            RegisterMessageType<EventMessage>();
            ReceiveMessage(receivedMessage);
            Assert.IsNotNull(ResultingException, "When no handlers are found and a message ends up in the endpoint, an exception should be thrown");
            Assert.That(ResultingException.GetBaseException().Message, Contains.Substring(typeof(EventMessage).ToString()), "The exception message should be meaningful and should inform the user the message type for which a handler could not be found.");
        }
    }
  
}
