namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Satellites;

    class ExecuteSatelliteHandlerBehavior: HomomorphicBehavior<IncomingContext>
    {
        public override void DoInvoke(IncomingContext context, Action next)
        {
            var satellite = context.Get<ISatellite>();

            context.Set("TransportReceiver.MessageHandledSuccessfully", satellite.Handle(context.PhysicalMessage));
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("SatelliteHandlerExecutor", typeof(ExecuteSatelliteHandlerBehavior), "Invokes the decryption logic")
            {
                InsertAfter("ReceiveMessage");
                InsertBeforeIfExists("FirstLevelRetries");
            }
        }
    }
}