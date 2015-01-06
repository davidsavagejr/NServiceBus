namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Linq;
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
            var outgoingContextType = typeof(OutgoingContext);

            foreach (var rego in model)
            {
                var behaviorInterface = rego.BehaviorType.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBehavior<,>));
                if (behaviorInterface.GetGenericArguments()[0] == outgoingContextType)
                {
                    Outgoing.Add(rego);
                }
                else
                {
                    Incoming.Add(rego);
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