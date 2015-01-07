namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using System.Linq;
    using Unicast.Messages;

    /// <summary>
    /// Incoming pipeline context.
    /// </summary>
    public class IncomingContext : PhysicalMessageProcessingContext
    {
        /// <summary>
        /// Enriches a processing context with deserialized logical messsages.
        /// </summary>
        /// <param name="logicalMessages">A collection of logical messages</param>
        /// <param name="parentContext">Atext wrapped con</param>
        public IncomingContext(IEnumerable<LogicalMessage> logicalMessages, PhysicalMessageProcessingContext parentContext)
            : base(parentContext)
        {
            handleCurrentMessageLaterWasCalled = false;
            LogicalMessages = logicalMessages.ToList();
        }

        /// <summary>
        /// Allows context inheritence.
        /// </summary>
        /// <param name="parentContext"></param>
        protected internal IncomingContext(BehaviorContext parentContext)
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