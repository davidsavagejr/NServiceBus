namespace NServiceBus
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Sagas;

    class InvokeHandlersBehavior : HomomorphicBehavior<HandlingContext>
    {
        public override void DoInvoke(HandlingContext context, Action next)
        {
            ActiveSagaInstance saga;

            if (context.TryGet(out saga) && saga.NotFound && saga.SagaType == context.MessageHandler.Instance.GetType())
            {
                next();
                return;
            }

            var messageHandler = context.MessageHandler;

            messageHandler.Invocation(messageHandler.Instance, context.IncomingLogicalMessage.Instance);
            next();
        }
    }
}