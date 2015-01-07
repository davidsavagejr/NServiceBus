namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// 
    /// </summary>
    public abstract class LogicalMessageProcessingStageBehavior : Behavior<LogicalMessageProcessingStageBehavior.Context>
    {
        /// <summary>
        /// Context for processing a single logical message
        /// </summary>
        public class Context : LogicalMessagesProcessingStageBehavior.Context
        {
            const string IncomingLogicalMessageKey = "NServiceBus.IncomingLogicalMessageKey";

            /// <summary>
            /// 
            /// </summary>
            /// <param name="logicalMessage">The logical message</param>
            /// <param name="parentContext">The wrapped context</param>
            public Context(LogicalMessage logicalMessage, LogicalMessagesProcessingStageBehavior.Context parentContext)
                : base(parentContext)
            {
                IncomingLogicalMessage = logicalMessage;
            }

            /// <summary>
            /// Allows context inheritence
            /// </summary>
            /// <param name="parentContext"></param>
            protected Context(BehaviorContext parentContext)
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
}