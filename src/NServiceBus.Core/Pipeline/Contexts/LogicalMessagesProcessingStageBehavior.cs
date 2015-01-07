namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using System.Linq;
    using Unicast.Messages;

    /// <summary>
    /// 
    /// </summary>
    public abstract class LogicalMessagesProcessingStageBehavior : Behavior<LogicalMessagesProcessingStageBehavior.Context>
    {
        /// <summary>
        /// Incoming pipeline context.
        /// </summary>
        public class Context : PhysicalMessageProcessingStageBehavior.Context
        {
            /// <summary>
            /// Enriches a processing context with deserialized logical messsages.
            /// </summary>
            /// <param name="logicalMessages">A collection of logical messages</param>
            /// <param name="parentContext">Atext wrapped con</param>
            public Context(IEnumerable<LogicalMessage> logicalMessages, PhysicalMessageProcessingStageBehavior.Context parentContext)
                : base(parentContext)
            {
                handleCurrentMessageLaterWasCalled = false;
                LogicalMessages = logicalMessages.ToList();
            }

            /// <summary>
            /// Allows context inheritence.
            /// </summary>
            /// <param name="parentContext"></param>
            protected internal Context(BehaviorContext parentContext)
                : base(parentContext)
            {

            }

            /// <summary>
            /// The received logical messages.
            /// </summary>
            public List<LogicalMessage> LogicalMessages
            {
                get { return Get<List<LogicalMessage>>(); }
                private set { Set(value); }
            }
        }
    }
}