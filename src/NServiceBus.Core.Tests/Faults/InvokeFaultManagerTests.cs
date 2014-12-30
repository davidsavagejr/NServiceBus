namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Core.Tests.Features;
    using NServiceBus.Faults;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class InvokeFaultManagerTests
    {
        [Test]
        public void ShouldInvokeTheSerializtionFailedForSerializationExceptions()
        {
            var faultManager = new FakeFaultManager();
            var behavior = new InvokeFaultManagerBehavior(new CriticalError((s,e)=>{},new FakeBuilder()));

            behavior.Invoke(CreateContext("someid", faultManager), () =>
            {
                throw new MessageDeserializationException("testex");
            });

            Assert.True(faultManager.SerializationFailedCalled);
            Assert.AreEqual("testex",faultManager.Exception.Message);
            Assert.AreEqual("someid", faultManager.Message.Id);
        }

        [Test]
        public void ShouldInvokeTheProcessingFailedForExceptions()
        {
            var faultManager = new FakeFaultManager();
            var behavior = new InvokeFaultManagerBehavior(new CriticalError((s, e) => { }, new FakeBuilder()));

            behavior.Invoke(CreateContext("someid", faultManager), () =>
            {
                throw new Exception("testex");
            });

            Assert.True(faultManager.ProcessingFailedCalled);
            Assert.AreEqual("testex", faultManager.Exception.Message);
            Assert.AreEqual("someid", faultManager.Message.Id);
        }
        [Test]
        public void ShouldInvokeCriticalErrorIfForwardingFails()
        {
            var faultManager = new FakeFaultManager
            {
                ThrowWhenCalled =true
            };
            var criticalErrorCalled = false;

            var behavior = new InvokeFaultManagerBehavior(new CriticalError((s, e) =>
            {
                criticalErrorCalled = true;
            }, new FakeBuilder()));


            //the ex should bubble to force the transport to rollback. If not the message will be lost
            Assert.Throws<Exception>(()=>behavior.Invoke(CreateContext("someid", faultManager), () =>
            {
                throw new Exception("testex");
            }));

            Assert.True(criticalErrorCalled);
        }



        IncomingContext CreateContext(string messageId, IManageMessageFailures faultManager)
        {
            var context = new IncomingContext(null);
            context.Set(faultManager);
            context.Set(IncomingContext.IncomingPhysicalMessageKey, new TransportMessage(messageId, new Dictionary<string, string>()));

            return context;
        }

        public class FakeFaultManager:IManageMessageFailures
        {
            public bool SerializationFailedCalled { get; set; }
            public bool ProcessingFailedCalled { get; set; }
            public Exception Exception { get; set; }
            public TransportMessage Message { get; set; }
            public bool ThrowWhenCalled { get; set; }

            public void SerializationFailedForMessage(TransportMessage message, Exception e)
            {
                Exception = e;
                Message = message;
                SerializationFailedCalled = true;

                if (ThrowWhenCalled)
                {
                    throw new Exception("Fault manager blew uo");
                }
            }

            public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
            {

                Exception = e;
                Message = message;
                ProcessingFailedCalled = true;

                if (ThrowWhenCalled)
                {
                    throw new Exception("Fault manager blew uo");
                }
            }

            public void Init(Address address)
            {
                
            }
        }
    }
}