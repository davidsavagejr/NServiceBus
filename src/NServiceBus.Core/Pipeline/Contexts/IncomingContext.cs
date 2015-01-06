namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using System.Linq;
    using Unicast.Messages;

    /// <summary>
    /// Incoming pipeline context.
    /// </summary>
    public class IncomingContext : AbortableContext
    {

        internal IncomingContext(IEnumerable<LogicalMessage> logicalMessages, BehaviorContext parentContext)
            : base(parentContext)
        {
            handleCurrentMessageLaterWasCalled = false;
            LogicalMessages = logicalMessages.ToList();
        }

        /// <summary>
        /// 
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