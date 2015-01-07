namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline.Contexts;
    using Pipeline;

    class SetCurrentMessageBeingHandledBehavior : Behavior<HandlingContext>
    {
        public override void Invoke(HandlingContext context, Action next)
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