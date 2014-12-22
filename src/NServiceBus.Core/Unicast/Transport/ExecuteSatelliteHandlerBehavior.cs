namespace NServiceBus.Unicast.Transport
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Satellites;

    class ExecuteSatelliteHandlerBehavior: IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
        {
            var satellite = context.Get<ISatellite>();

            context.Set("TransportReceiver.MessageHandledSuccessfully", satellite.Handle(context.PhysicalMessage));
        }

        public class ExecuteSatelliteHandlerBehaviorRegistration : RegisterStep
        {
            public ExecuteSatelliteHandlerBehaviorRegistration()
                : base("SatelliteHandlerExecutor", typeof(ExecuteSatelliteHandlerBehavior), "Invokes the decryption logic")
            {
                InsertBefore("HandlerTransactionScopeWrapperBehavior");
            }
        }
    }
}