namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// Context for processing a single logical message
    /// </summary>
    public class IncomingLogicalMessageContext : IncomingContext
    {
        const string IncomingLogicalMessageKey = "NServiceBus.IncomingLogicalMessageKey";

        internal IncomingLogicalMessageContext(LogicalMessage logicalMessage, BehaviorContext parentContext) : base(parentContext)
        {
            IncomingLogicalMessage = logicalMessage;
        }

        /// <summary>
        /// Creates a new context
        /// </summary>
        /// <param name="parentContext"></param>
        protected IncomingLogicalMessageContext(IncomingLogicalMessageContext parentContext)
            : base(parentContext)
        {
        }

        /// <summary>
        /// The current logical message being processed.
        /// </summary>
        public LogicalMessage IncomingLogicalMessage
        {
            get { return Get<LogicalMessage>(IncomingLogicalMessageKey); }
            private set { Set(IncomingLogicalMessageKey, value); }
        }
    }
}