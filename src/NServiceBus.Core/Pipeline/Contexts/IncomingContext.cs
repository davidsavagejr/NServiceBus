namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using Unicast.Messages;

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

    /// <summary>
    /// Incoming pipeline context.
    /// </summary>
    public class IncomingContext : BehaviorContext
    {
        internal IncomingContext(BehaviorContext parentContext)
            : base(parentContext)
        {
            handleCurrentMessageLaterWasCalled = false;

            LogicalMessages = new List<LogicalMessage>();
        }

        /// <summary>
        /// <code>true</code> if DoNotInvokeAnyMoreHandlers has been called.
        /// </summary>
        public bool HandlerInvocationAborted { get; private set; }

        /// <summary>
        /// Call this to stop the invocation of handlers.
        /// </summary>
        public void DoNotInvokeAnyMoreHandlers()
        {
            HandlerInvocationAborted = true;
        }

        /// <summary>
        /// The received message.
        /// </summary>
        public TransportMessage PhysicalMessage
        {
            get { return Get<TransportMessage>(IncomingPhysicalMessageKey); }
            set { Set(IncomingPhysicalMessageKey, value); }
        }
        
        /// <summary>
        /// The received logical messages.
        /// </summary>
        public List<LogicalMessage> LogicalMessages
        {
            get { return Get<List<LogicalMessage>>(); }
            set { Set(value); }
        }

        
        internal const string IncomingPhysicalMessageKey = "NServiceBus.IncomingPhysicalMessage";
    }
}