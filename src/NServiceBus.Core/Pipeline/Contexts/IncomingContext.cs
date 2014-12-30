﻿namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using Unicast.Behaviors;
    using Unicast.Messages;

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

        /// <summary>
        /// The current logical message being processed.
        /// </summary>
        public LogicalMessage IncomingLogicalMessage
        {
            get { return Get<LogicalMessage>(IncomingLogicalMessageKey); }
            set { Set(IncomingLogicalMessageKey, value); }
        }

        /// <summary>
        /// The current <see cref="IHandleMessages{T}"/> being executed.
        /// </summary>
        public MessageHandler MessageHandler
        {
            get { return Get<MessageHandler>(); }
            set { Set(value); }
        }

        internal const string IncomingPhysicalMessageKey = "NServiceBus.IncomingPhysicalMessage";
        const string IncomingLogicalMessageKey = "NServiceBus.IncomingLogicalMessageKey";
    }
}