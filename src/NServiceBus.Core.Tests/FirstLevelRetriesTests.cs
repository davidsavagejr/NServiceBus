﻿namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.FirstLevelRetries;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class FirstLevelRetriesTests
    {
        [Test]
        public void ShouldNotPerformFLROnMessagesThatCantBeDeserialized()
        {
            var behavior = FirstLevelRetriesBehavior.CreateForTests(null, new FirstLevelRetryPolicy(0), new BusNotifications());

            Assert.Throws<MessageDeserializationException>(() => behavior.DoInvoke(null, () =>
            {
                throw new MessageDeserializationException("test");
            }));
        }

        [Test]
        public void ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var behavior = FirstLevelRetriesBehavior.CreateForTests(new FlrStatusStorage(), new FirstLevelRetryPolicy(1), new BusNotifications());
            var context = CreateContext("someid");

            behavior.DoInvoke(context, () =>
            {
                throw new Exception("test"); 
            });

            Assert.False(context.MessageHandledSuccessfully());
        }

        [Test]
        public void ShouldBubbleTheExceptionUpIfThereAreNoMoreRetriesLeft()
        {
            var behavior = FirstLevelRetriesBehavior.CreateForTests(new FlrStatusStorage(), new FirstLevelRetryPolicy(0), new BusNotifications());
            var context = CreateContext("someid");

            Assert.Throws<Exception>(() => behavior.DoInvoke(context, () =>
            {
                throw new Exception("test");
            }));

            //should set the retries header to capture how many flr attempts where made
            Assert.AreEqual("0", context.PhysicalMessage.Headers[Headers.FLRetries]);
        }

        [Test]
        public void ShouldClearStorageAfterGivingUp()
        {
            var storage = new FlrStatusStorage();
            var behavior = FirstLevelRetriesBehavior.CreateForTests(storage, new FirstLevelRetryPolicy(1), new BusNotifications());

            storage.IncrementFailuresForMessage("someid", new Exception(""));

            Assert.Throws<Exception>(() => behavior.DoInvoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            }));


            Assert.AreEqual(0, storage.GetRetriesForMessage("someid"));
        }
        [Test]
        public void ShouldRememberRetryCountBetweenRetries()
        {
            var storage = new FlrStatusStorage();
            var behavior = FirstLevelRetriesBehavior.CreateForTests(storage, new FirstLevelRetryPolicy(1), new BusNotifications());

            behavior.DoInvoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            });


            Assert.AreEqual(1, storage.GetRetriesForMessage("someid"));
        }


        [Test]
        public void ShouldRaiseBusNotificationsForFLR()
        {
            var notifications = new BusNotifications();
            var storage = new FlrStatusStorage();
            var behavior = FirstLevelRetriesBehavior.CreateForTests(storage, new FirstLevelRetryPolicy(1), notifications);

            var notificationFired = false;

            notifications.Errors.MessageHasFailedAFirstLevelRetryAttempt.Subscribe(flr =>
            {
                Assert.AreEqual(0, flr.RetryAttempt);
                Assert.AreEqual("test", flr.Exception.Message);
                Assert.AreEqual("someid", flr.Headers[Headers.MessageId]);

                notificationFired = true;
            })
                ;
            behavior.DoInvoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            });


            Assert.True(notificationFired);
        }
        IncomingContext CreateContext(string messageId)
        {
            var context = new IncomingContext(null);

            context.Set(IncomingContext.IncomingPhysicalMessageKey, new TransportMessage(messageId, new Dictionary<string, string>()));

            return context;
        }
    }
}