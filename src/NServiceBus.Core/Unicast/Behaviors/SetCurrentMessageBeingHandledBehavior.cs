namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline.Contexts;
    using Pipeline;

    class SetCurrentMessageBeingHandledBehavior : HomomorphicBehavior<HandlingContext>
    {
        public override void DoInvoke(HandlingContext context, Action next)
        {
            var logicalMessage = context.IncomingLogicalMessage;

            try
            {
                ExtensionMethods.CurrentMessageBeingHandled = logicalMessage.Instance;

                next();
            }
            finally
            {
                ExtensionMethods.CurrentMessageBeingHandled = null;
            }
        }
    }
}