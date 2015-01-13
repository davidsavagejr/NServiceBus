namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Linq;
    using Contexts;
    using ObjectBuilder;
    using Settings;

    /// <summary>
    ///     Orchestrates the execution of a pipeline.
    /// </summary>
    public class PipelineExecutor
    {
        /// <summary>
        ///     Create a new instance of <see cref="PipelineExecutor" />.
        /// </summary>
        /// <param name="settings">The settings to read data from.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="busNotifications">Bus notifications.</param>
        public PipelineExecutor(ReadOnlySettings settings, IBuilder builder, BusNotifications busNotifications)
            : this(builder, busNotifications, settings.Get<PipelineModifications>(), builder.Build<BehaviorContextStacker>())
        {
        }

        internal PipelineExecutor(IBuilder builder, BusNotifications busNotifications, PipelineModifications pipelineModifications, BehaviorContextStacker contextStacker)
        {
            this.busNotifications = busNotifications;
            this.contextStacker = contextStacker;

            var pipelineBuilder = new PipelineBuilder(pipelineModifications);
            Incoming = pipelineBuilder.Incoming.AsReadOnly();
            Outgoing = pipelineBuilder.Outgoing.AsReadOnly();

            incomingBehaviors = Incoming.Select(r => r.CreateBehavior(builder)).ToArray();
            outgoingBehaviors = Outgoing.Select(r => r.CreateBehavior(builder)).ToArray();
        }

        /// <summary>
        ///     The list of incoming steps registered.
        /// </summary>
        public IList<RegisterStep> Incoming { get; private set; }

        /// <summary>
        ///     The list of outgoing steps registered.
        /// </summary>
        public IList<RegisterStep> Outgoing { get; private set; }

        internal void InvokeSendPipeline(OutgoingContext context)
        {            
            InvokePipeline(outgoingBehaviors, context);
        }
        
        internal void InvokeReceivePipeline(IncomingContext context)
        {
            InvokePipeline(incomingBehaviors, context);
        }

        void InvokePipeline<TContext>(IEnumerable<BehaviorInstance> behaviors, TContext context) where TContext : BehaviorContext
        {
            var pipeline = new BehaviorChain(behaviors, context, this, busNotifications);
            pipeline.Invoke(contextStacker);
        }


        readonly BehaviorContextStacker contextStacker;
        IEnumerable<BehaviorInstance> incomingBehaviors;
        IEnumerable<BehaviorInstance> outgoingBehaviors;
        readonly BusNotifications busNotifications;
    }
}
