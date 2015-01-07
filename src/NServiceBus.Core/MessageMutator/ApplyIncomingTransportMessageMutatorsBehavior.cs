namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using Pipeline;
    using Pipeline.Contexts;


    class ApplyIncomingTransportMessageMutatorsBehavior : HomomorphicBehavior<PhysicalMessageProcessingContext>
    {
        public override void DoInvoke(PhysicalMessageProcessingContext context, Action next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();

            foreach (var mutator in mutators)
            {
                mutator.MutateIncoming(context.PhysicalMessage);
            }

            next();
        }
    }
}