namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class TransportReceiveToPhysicalMessageProcessingConnector : StageConnector<TransportReceiveContext, PhysicalMessageProcessingStageBehavior.Context>
    {
        public override void Invoke(TransportReceiveContext context, Action<PhysicalMessageProcessingStageBehavior.Context> next)
        {
            var abortableContext = new PhysicalMessageProcessingStageBehavior.Context(context);
            next(abortableContext);
            if (!abortableContext.MessageHandledSuccessfully)
            {
                throw new MessageProcessingAbortedException();
            }
        }
    }
}