namespace NServiceBus
{
    using System;
    using System.Linq;
    using MessageInterfaces;
    using NServiceBus.Unicast.Behaviors;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class LoadHandlersBehavior : StageConnector<LogicalMessageProcessingStageBehavior.Context, HandlingContext>
    {
        public IMessageHandlerRegistry HandlerRegistry { get; set; }

        public IMessageMapper MessageMapper { get; set; }

        public PipelineExecutor PipelineFactory { get; set; }

        public override void Invoke(LogicalMessageProcessingStageBehavior.Context context, Action<HandlingContext> next)
        {
            var messageToHandle = context.IncomingLogicalMessage;

            // for now we cheat and pull it from the behavior context:
            var callbackInvoked = context.Get<bool>(CallbackInvocationBehavior.CallbackInvokedKey);

            var handlerTypedToInvoke = HandlerRegistry.GetHandlerTypes(messageToHandle.MessageType).ToList();

            if (!callbackInvoked && !handlerTypedToInvoke.Any())
            {
                var error = string.Format("No handlers could be found for message type: {0}", messageToHandle.MessageType);
                throw new InvalidOperationException(error);
            }

            foreach (var handlerType in handlerTypedToInvoke)
            {
                var loadedHandler = new MessageHandler
                {
                    Instance = context.Builder.Build(handlerType),
                    Invocation = (handlerInstance, message) => HandlerRegistry.InvokeHandle(handlerInstance, message)
                };

                var handlingContext = new HandlingContext(loadedHandler, context);
                next(handlingContext);

                if (handlingContext.HandlerInvocationAborted)
                {
                    //if the chain was aborted skip the other handlers
                    break;
                }
            }
        }
    }
}