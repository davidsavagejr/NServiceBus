namespace NServiceBus
{
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Behaviors;

    /// <summary>
    /// A context of handling a logical message by a handler
    /// </summary>
    public class HandlingContext : IncomingLogicalMessageContext
    {
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
    }
}