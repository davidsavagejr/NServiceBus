namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using Pipeline;
    using Pipeline.Contexts;

    class MutateOutgoingPhysicalMessageBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                mutator.MutateOutgoing(context.OutgoingLogicalMessage, context.OutgoingMessage);
            }

            next();
        }
    }
}