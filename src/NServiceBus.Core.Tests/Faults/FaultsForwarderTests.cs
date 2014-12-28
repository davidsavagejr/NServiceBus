namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Faults;
    using NServiceBus.Faults.Forwarder;
    using NServiceBus.Hosting;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    [TestFixture]
    public class FaultsForwarderTests
    {
        [Test]
        public void ShouldForwardMessagesThatAlwaysFails()
        {
            var sender = new FakeSender();

            var faultManager = (IManageMessageFailures)new ForwardingFaultManager(sender,"error",new HostInformation(Guid.NewGuid(),"my host"), new BusNotifications());
 
            faultManager.Init(Address.Parse("local-address"));

            faultManager.ProcessingAlwaysFailsForMessage(new TransportMessage("someid",new Dictionary<string, string>()), new Exception("testex"));

            Assert.AreEqual("someid", sender.MessageSent.Id);
        }

        [Test]
        public void ShouldForwardMessagesThatFailsDeserialization()
        {
            var sender = new FakeSender();

            var faultManager = (IManageMessageFailures)new ForwardingFaultManager(sender, "error", new HostInformation(Guid.NewGuid(), "my host"), new BusNotifications());

            faultManager.Init(Address.Parse("local-address"));

            faultManager.SerializationFailedForMessage(new TransportMessage("someid", new Dictionary<string, string>()), new Exception("testex"));

            Assert.AreEqual("someid", sender.MessageSent.Id);
        }

        [Test]
        public void ShouldBubbleFailuresWhenSending()
        {
            var sender = new FakeSender
            {
                ThrowOnSend =true
            };

            var faultManager = (IManageMessageFailures)new ForwardingFaultManager(sender, "error", new HostInformation(Guid.NewGuid(), "my host"), new BusNotifications());

            faultManager.Init(Address.Parse("local-address"));

            Assert.Throws<InvalidOperationException>(()=>faultManager.SerializationFailedForMessage(new TransportMessage("someid", new Dictionary<string, string>()), new Exception("testex")));

        }

        [Test]
        public void ShouldRaiseNotificationWhenMessageIsForwarded()
        {
            var sender = new FakeSender();
            var notifications = new BusNotifications();

            var failedMessageNotification = new FailedMessage();

            notifications.Errors.MessageSentToErrorQueue.Subscribe(f => { failedMessageNotification = f; });

            var faultManager = (IManageMessageFailures)new ForwardingFaultManager(sender, "error", new HostInformation(Guid.NewGuid(), "my host"), notifications);

            faultManager.Init(Address.Parse("local-address"));

            faultManager.SerializationFailedForMessage(new TransportMessage("someid", new Dictionary<string, string>()), new Exception("testex"));

            Assert.AreEqual("someid", failedMessageNotification.Headers[Headers.MessageId]);

            Assert.AreEqual("testex", failedMessageNotification.Exception.Message);
        }


        [Test]
        public void ShouldEnrichHeadersWithHostAndExceptionDetails()
        {
            var sender = new FakeSender();
            var hostInfo = new HostInformation(Guid.NewGuid(), "my host");

            var faultManager = (IManageMessageFailures)new ForwardingFaultManager(sender, "error", hostInfo, new BusNotifications());

            faultManager.Init(Address.Parse("local-address"));

            faultManager.SerializationFailedForMessage(new TransportMessage("someid", new Dictionary<string, string>()), new Exception("testex"));

            //host info
            Assert.AreEqual(hostInfo.HostId.ToString("N"), sender.MessageSent.Headers[Headers.HostId]);
            Assert.AreEqual(hostInfo.DisplayName, sender.MessageSent.Headers[Headers.HostDisplayName]);

            //exception details
            Assert.AreEqual("testex", sender.MessageSent.Headers["NServiceBus.ExceptionInfo.Message"]);
            
        }
        public class FakeSender : ISendMessages
        {
            public TransportMessage MessageSent { get; set; }

            public SendOptions OptionsUsed { get; set; }
            public bool ThrowOnSend { get; set; }

            public void Send(TransportMessage message, SendOptions sendOptions)
            {
                MessageSent = message;
                OptionsUsed = sendOptions;

                if (ThrowOnSend)
                {
                    throw new Exception("Failed to send");
                }
            }
        }
    }
}