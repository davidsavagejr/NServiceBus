﻿namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Contexts;
    using ObjectBuilder;
    using Settings;
    using Unicast;
    using Unicast.Messages;

    /// <summary>
    ///     Orchestrates the execution of a pipeline.
    /// </summary>
    public class PipelineExecutor : IDisposable
    {
        /// <summary>
        ///     Create a new instance of <see cref="PipelineExecutor" />.
        /// </summary>
        /// <param name="settings">The settings to read data from.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="busNotifications">Bus notifications.</param>
        public PipelineExecutor(ReadOnlySettings settings, IBuilder builder, BusNotifications busNotifications)
            : this(builder, busNotifications, settings.Get<PipelineModifications>())
        {
        }

        internal PipelineExecutor(IBuilder builder, BusNotifications busNotifications, PipelineModifications pipelineModifications)
        {
            rootBuilder = builder;
            this.busNotifications = busNotifications;

            var pipelineBuilder = new PipelineBuilder(pipelineModifications);
            Incoming = pipelineBuilder.Incoming.AsReadOnly();
            Outgoing = pipelineBuilder.Outgoing.AsReadOnly();

            incomingBehaviors = Incoming.Select(r => r.CreateBehavior<IncomingContext>(builder)).ToArray();
            outgoingBehaviors = Outgoing.Select(r => r.CreateBehavior<OutgoingContext>(builder)).ToArray();
        }

        /// <summary>
        ///     The list of incoming steps registered.
        /// </summary>
        public IList<RegisterStep> Incoming{ get; private set; }

        /// <summary>
        ///     The list of outgoing steps registered.
        /// </summary>
        public IList<RegisterStep> Outgoing { get; private set; }

        /// <summary>
        ///     The current context being executed.
        /// </summary>
        public BehaviorContext CurrentContext
        {
            get
            {
                var current = contextStacker.Current;

                if (current != null)
                {
                    return current;
                }

                contextStacker.Push(new RootContext(rootBuilder));

                return contextStacker.Current;
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            //Injected
        }

        /// <summary>
        ///     Invokes a chain of behaviors.
        /// </summary>
        /// <typeparam name="TContext">The context to use.</typeparam>
        /// <param name="behaviors">The behaviors to execute in the specified order.</param>
        /// <param name="context">The context instance.</param>
        void InvokePipeline<TContext>(IEnumerable<IBehaviorInstance<TContext>> behaviors, TContext context) where TContext : BehaviorContext
        {
            var pipeline = new BehaviorChain<TContext>(behaviors, context, this, busNotifications);

            Execute(pipeline, context);
        }

        internal void InvokeReceivePhysicalMessagePipeline(IncomingContext context)
        {
            InvokePipeline(incomingBehaviors, context);
        }

        internal OutgoingContext InvokeSendPipeline(DeliveryOptions deliveryOptions, LogicalMessage message)
        {
            var context = new OutgoingContext(CurrentContext, deliveryOptions, message);

            InvokePipeline(outgoingBehaviors, context);

            return context;
        }

        void DisposeManaged()
        {
            contextStacker.Dispose();
        }

        void Execute<T>(BehaviorChain<T> pipelineAction, T context) where T : BehaviorContext
        {
            try
            {
                contextStacker.Push(context);
                pipelineAction.Invoke();
            }
            finally
            {
                contextStacker.Pop();
            }
        }

        BehaviorContextStacker contextStacker = new BehaviorContextStacker();
        IEnumerable<IBehaviorInstance<IncomingContext>> incomingBehaviors;
        IEnumerable<IBehaviorInstance<OutgoingContext>> outgoingBehaviors;
        IBuilder rootBuilder;
        readonly BusNotifications busNotifications;
    }
}
