namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Linq;

    class PipelineRuntimeModel
    {
        readonly IList<RegisterStep> incomingSteps;
        readonly IList<RegisterStep> outgoingSteps;

        public PipelineRuntimeModel(IEnumerable<RegisterStep> incomingSteps, IEnumerable<RegisterStep> outgoingSteps)
        {
            this.incomingSteps = incomingSteps.ToList();
            this.outgoingSteps = outgoingSteps.ToList();
        }

        public IList<RegisterStep> IncomingSteps
        {
            get { return incomingSteps; }
        }

        public IList<RegisterStep> OutgoingSteps
        {
            get { return outgoingSteps; }
        }
    }
}