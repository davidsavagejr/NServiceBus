namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.ObjectBuilder;

    class StaticBehavior<TContext> : IBehaviorInstance<TContext> 
        where TContext : BehaviorContext
    {
        readonly Type type;
        readonly Lazy<IBehavior<TContext>> lazyInstance; 

        public StaticBehavior(Type type, IBuilder defaultBuilder)
        {
            this.type = type;
            lazyInstance = new Lazy<IBehavior<TContext>>(() => (IBehavior<TContext>)defaultBuilder.Build(type));
        }

        public Type Type
        {
            get { return type; }
        }

        public IBehavior<TContext> GetInstance(IBuilder contextBuilder)
        {
            return lazyInstance.Value;
        }
    }
}