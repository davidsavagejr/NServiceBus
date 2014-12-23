namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Contexts;

    class PipelineBuilder
    {
        public PipelineBuilder(PipelineModifications modifications)
        {
            coordinator = new StepRegistrationsCoordinator(modifications.Removals, modifications.Replacements);

            RegisterAdditionalBehaviors(modifications.Additions);

            var model = coordinator.BuildRuntimeModel();

            Incoming = new List<RegisterStep>();
            Outgoing = new List<RegisterStep>();
            var behaviorType = typeof(IBehavior<>);
            var outgoingContextType = typeof(OutgoingContext);
            var incomingContextType = typeof(IncomingContext);

            foreach (var rego in model)
            {
                if (behaviorType.MakeGenericType(incomingContextType).IsAssignableFrom(rego.BehaviorType))
                {
                    Incoming.Add(rego);
                }

                if (behaviorType.MakeGenericType(outgoingContextType).IsAssignableFrom(rego.BehaviorType))
                {
                    Outgoing.Add(rego);
                }
            }
        }

        public List<RegisterStep> Incoming { get; private set; }
        public List<RegisterStep> Outgoing { get; private set; }

        void RegisterAdditionalBehaviors(List<RegisterStep> additions)
        {
            foreach (var rego in additions)
            {
                coordinator.Register(rego);
            }
        }

 

        StepRegistrationsCoordinator coordinator;
    }
}