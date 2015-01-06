namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport.Monitoring;

    class ReceivePerformanceDiagnosticsBehavior : HomomorphicBehavior<AbortableContext>
    {
        public override void DoInvoke(AbortableContext context, Action next)
        {
            context.Get<ReceivePerformanceDiagnostics>().MessageDequeued();
            next();
        }
    }
}