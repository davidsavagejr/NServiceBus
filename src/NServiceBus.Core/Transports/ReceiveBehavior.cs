namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// 
    /// </summary>
    public abstract class ReceiveBehavior : IBehavior<BootstrapContext, PhysicalMessageContext>
    {
        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TIn,TOut}"/> in the chain to execute.</param>
        public void Invoke(BootstrapContext context, Action<PhysicalMessageContext> next)
        {
            Invoke(context, x => next(new PhysicalMessageContext(x, context)));
        }

        //TODO: change to header and body ony
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="onMessage"></param>
        protected abstract void Invoke(BootstrapContext context, Action<TransportMessage> onMessage);
    }

    class ReceiveBehaviorRegistration : RegisterStep
    {
        public ReceiveBehaviorRegistration()
            : base("ReceiveMessage", typeof(ReceiveBehavior), "Try receive message from transport", false)
        {
        }
    }
}