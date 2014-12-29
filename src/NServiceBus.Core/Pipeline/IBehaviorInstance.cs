namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface IBehaviorInstance<in TContext>
        where TContext : BehaviorContext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextBuilder"></param>
        /// <returns></returns>
        IBehavior<TContext> GetInstance(IBuilder contextBuilder);
        /// <summary>
        /// 
        /// </summary>
        Type Type { get; }
    }
}