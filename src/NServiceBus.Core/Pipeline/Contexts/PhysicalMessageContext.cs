namespace NServiceBus.Pipeline.Contexts
{
    /// <summary>
    /// Context containing a physical message
    /// </summary>
    public class PhysicalMessageContext : BootstrapContext
    {
        internal const string IncomingPhysicalMessageKey = "NServiceBus.IncomingPhysicalMessage";

        /// <summary>
        /// Context containing a physical message
        /// </summary>
        /// <param name="physicalMessage"></param>
        /// <param name="parentContext"></param>
        internal PhysicalMessageContext(TransportMessage physicalMessage, BehaviorContext parentContext) 
            : base(parentContext)
        {
            PhysicalMessage = physicalMessage;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentContext"></param>
        protected PhysicalMessageContext(BehaviorContext parentContext)
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