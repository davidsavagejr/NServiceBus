namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Unicast.Behaviors;

    /// <summary>
    /// A context of handling a logical message by a handler
    /// </summary>
    public class HandlingContext : IncomingLogicalMessageContext
    {
        const string HandlerInvocationAbortedKey = "NServiceBus.HandlerInvocationAborted";

        internal HandlingContext(MessageHandler handler, IncomingLogicalMessageContext parentContext)
            : base(parentContext)
        {
            Set(handler);
        }

        /// <summary>
        /// The current <see cref="IHandleMessages{T}"/> being executed.
        /// </summary>
        public MessageHandler MessageHandler
        {
            get { return Get<MessageHandler>(); }
        }

        /// <summary>
        /// Call this to stop the invocation of handlers.
        /// </summary>
        public void DoNotInvokeAnyMoreHandlers()
        {
            HandlerInvocationAborted = true;
        }

        /// <summary>
        /// <code>true</code> if DoNotInvokeAnyMoreHandlers has been called.
        /// </summary>
        public bool HandlerInvocationAborted
        {
            get
            {
                bool handlerInvocationAborted;

                if (TryGet(HandlerInvocationAbortedKey, out handlerInvocationAborted))
                {
                    return handlerInvocationAborted;
                }
                return false;
            }
            private set { Set(HandlerInvocationAbortedKey,value); }
        }
    }
}