namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.ObjectBuilder;

    interface IBehaviorInstance<in TContext>
        where TContext : BehaviorContext
    {
        IBehavior<TContext> GetInstance(IBuilder contextBuilder);
        Type Type { get; }
    }
}