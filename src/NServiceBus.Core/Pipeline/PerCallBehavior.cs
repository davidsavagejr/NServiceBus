namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.ObjectBuilder;

    class PerCallBehavior<TContext> : IBehaviorInstance<TContext> 
        where TContext : BehaviorContext
    {
        readonly Type type;

        public PerCallBehavior(Type type)
        {
            this.type = type;
        }

        public Type Type
        {
            get { return type; }
        }

        public IBehavior<TContext> GetInstance(IBuilder contextBuilder)
        {
            return (IBehavior<TContext>) contextBuilder.Build(Type);
        }
    }
}