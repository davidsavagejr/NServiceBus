namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.ObjectBuilder;

    class PerCallBehavior : BehaviorInstance
    {
        public PerCallBehavior(Type type) : base(type)
        {
        }

        public override object GetInstance(IBuilder contextBuilder)
        {
            return contextBuilder.Build(Type);
        }
    }
}