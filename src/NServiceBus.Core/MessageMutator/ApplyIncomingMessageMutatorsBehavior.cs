namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline.Contexts;


    class ApplyIncomingMessageMutatorsBehavior : LogicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var current = context.IncomingLogicalMessage.Instance;

            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingMessages>())
            {
                //message mutators may need to assume that this has been set (eg. for the purposes of headers).
                ExtensionMethods.CurrentMessageBeingHandled = current;
                current = mutator.MutateIncoming(current);
                context.IncomingLogicalMessage.UpdateMessageInstance(current);
            }

            next();
        }
    }
}