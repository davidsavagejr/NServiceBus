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
            var behavior = new FirstLevelRetriesBehavior(null,0);
            
           Assert.Throws<MessageDeserializationException>(()=> behavior.Invoke(null, () => {
                    throw new MessageDeserializationException("test");
            }));
        }

        [Test]
        public void ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var behavior = new FirstLevelRetriesBehavior(new FlrStatusStorage(null),1);
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
            var behavior = new FirstLevelRetriesBehavior(new FlrStatusStorage(null),0);
            var context = new IncomingContext(null);

            context.Set(IncomingContext.IncomingPhysicalMessageKey, new TransportMessage("someid", new Dictionary<string, string>()));

            Assert.Throws<Exception>(() => behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            }));
        }

        [Test]
        public void ShouldClearStorageAfterGivingUp()
        {
            var storage = new FlrStatusStorage(null);
            var behavior = new FirstLevelRetriesBehavior(storage,1);

            storage.IncrementFailuresForMessage("someid",new Exception(""));
          
            Assert.Throws<Exception>(() => behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            }));


            Assert.AreEqual(0, storage.GetRetriesForMessage("someid"));
        }
        [Test]
        public void ShouldRememberRetryCountBetweenRetries()
        {
            var storage = new FlrStatusStorage(null);
            var behavior = new FirstLevelRetriesBehavior(storage, 1);

            behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            });


            Assert.AreEqual(1, storage.GetRetriesForMessage("someid"));
        }

        IncomingContext CreateContext(string messageId)
        {
            var context = new IncomingContext(null);

            context.Set(IncomingContext.IncomingPhysicalMessageKey, new TransportMessage(messageId, new Dictionary<string, string>()));

            return context;
        }
    }
}