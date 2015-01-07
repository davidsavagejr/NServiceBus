namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// Context for processing a single logical message
    /// </summary>
    public class IncomingLogicalMessageContext : IncomingContext
    {
        const string IncomingLogicalMessageKey = "NServiceBus.IncomingLogicalMessageKey";

        /// <summary>
        /// Enriches an <see cref="IncomingContext"/> with a logical message to be processed.
        /// </summary>
        /// <param name="logicalMessage">The logical message</param>
        /// <param name="parentContext">The wrapped context</param>
        public IncomingLogicalMessageContext(LogicalMessage logicalMessage, IncomingContext parentContext)
            : base(parentContext)
        {
            IncomingLogicalMessage = logicalMessage;
        }

        /// <summary>
        /// Allows context inheritence
        /// </summary>
        /// <param name="parentContext"></param>
        protected IncomingLogicalMessageContext(BehaviorContext parentContext)
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