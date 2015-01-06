namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    class FakeMessagePump : IDequeueMessages
    {
        readonly Observable<MessageAvailable> observable = new Observable<MessageAvailable>(); 

        public IDisposable Subscribe(IObserver<MessageAvailable> observer)
        {
            return observable.Subscribe(observer);
        }

        public void SignalMessageAvailable(TransportMessage fakeMessage)
        {
            observable.OnNext(new MessageAvailable("TestEndpoint", x => x.Set("FakeMessage",fakeMessage)));
        }

        public void Init(DequeueSettings settings)
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }

    class FakeReceiveBehavior : HomomorphicBehavior<IncomingContext>
    {
        public override void DoInvoke(IncomingContext context, Action next)
        {
            var msg = context.Get<TransportMessage>("FakeMessage");
            context.Set(IncomingContext.IncomingPhysicalMessageKey, msg);
            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base(WellKnownStep.Receive, typeof(FakeReceiveBehavior), "Receives a fake message")
            {
            }
        }
    }
}