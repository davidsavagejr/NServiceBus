namespace NServiceBus.Pipeline.Contexts
{
    /// <summary>
    /// Context containing a physical message
    /// </summary>
    public class PhysicalMessageReceiveContext : BootstrapContext
    {
        internal const string IncomingPhysicalMessageKey = "NServiceBus.IncomingPhysicalMessage";

        internal PhysicalMessageReceiveContext(TransportMessage physicalMessage, BehaviorContext parentContext) 
            : base(parentContext)
        {
            PhysicalMessage = physicalMessage;
        }

        /// <summary>
        /// Allows context inheritence
        /// </summary>
        /// <param name="parentContext"></param>
        protected PhysicalMessageReceiveContext(BehaviorContext parentContext)
            : base(parentContext)
        {
        }


        /// <summary>
        /// The received message.
        /// </summary>
        public TransportMessage PhysicalMessage
        {
            get { return Get<TransportMessage>(IncomingPhysicalMessageKey); }
            private set { Set(IncomingPhysicalMessageKey, value); }
        }
    }
}