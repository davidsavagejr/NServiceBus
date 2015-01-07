namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport.Monitoring;

    class ReceivePerformanceDiagnosticsBehavior : HomomorphicBehavior<PhysicalMessageProcessingContext>
    {
        public override void DoInvoke(PhysicalMessageProcessingContext context, Action next)
        {
            context.Get<ReceivePerformanceDiagnostics>().MessageDequeued();
            next();
        }
    }
}