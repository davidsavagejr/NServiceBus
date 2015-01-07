namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Connects two stages of the pipeline 
    /// </summary>
    /// <typeparam name="TFrom"></typeparam>
    /// <typeparam name="TTo"></typeparam>
    abstract class StageConnector<TFrom, TTo> : IBehavior<TFrom, TTo> where TFrom : BehaviorContext
        where TTo : BehaviorContext
    {
        public abstract void Invoke(TFrom context, Action<TTo> next);
    }
}