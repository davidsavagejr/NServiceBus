namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class FirstLevelRetriesTests
    {
        [Test]
        public void ShouldNotPerformFLROnMessagesThatCantBeDeserialized()
        {
            var behavior = new FirstLevelRetriesBehavior(null);
            
           Assert.Throws<MessageDeserializationException>(()=> behavior.Invoke(null, () => {
                    throw new MessageDeserializationException("test");
            }));
        }

        [Test]
        public void ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var behavior = new FirstLevelRetriesBehavior(new FlrStatusStorage(1,null,null));
            var context = new IncomingContext(null);

            context.Set(IncomingContext.IncomingPhysicalMessageKey,new TransportMessage("someid",new Dictionary<string, string>()));

            behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            });

            Assert.False(context.MessageHandledSuccessfully());
        }

        [Test]
        public void ShouldBubbleTheExceptionUpIfThereAreNoMoreRetriesLeft()
        {
            var behavior = new FirstLevelRetriesBehavior(new FlrStatusStorage(0, null, null));
            var context = new IncomingContext(null);

            context.Set(IncomingContext.IncomingPhysicalMessageKey, new TransportMessage("someid", new Dictionary<string, string>()));

            Assert.Throws<Exception>(() => behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            }));
        }
    }
}