namespace NServiceBus.Pipeline.Contexts
{
    /// <summary>
    /// 
    /// </summary>
    public class PhysicalMessageProcessingContext : PhysicalMessageReceiveContext
    {
        const string MessageHandledSuccessfullyKey = "TransportReceiver.MessageHandledSuccessfully";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentContext"></param>
        protected internal PhysicalMessageProcessingContext(BehaviorContext parentContext)
            : base(parentContext)
        {
            
        }

        /// <summary>
        /// True if the message was handled successfully and the MQ operations should be committed
        /// </summary>
        /// <value></value>
        public bool MessageHandledSuccessfully
        {
            get
            {
                bool messageHandledSuccessfully;

                if (!TryGet(MessageHandledSuccessfullyKey, out messageHandledSuccessfully))
                {
                    return true;
                }

                return messageHandledSuccessfully;
            }
        }


        /// <summary>
        /// Tells the transport to rollback the current receive operation
        /// </summary>
        public void AbortReceiveOperation()
        {
            Set(MessageHandledSuccessfullyKey, false);
        }
    }
}