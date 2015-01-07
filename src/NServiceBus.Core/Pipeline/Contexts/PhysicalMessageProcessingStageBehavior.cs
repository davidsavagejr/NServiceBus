namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// 
    /// </summary>
    public abstract class PhysicalMessageProcessingStageBehavior : Behavior<PhysicalMessageProcessingStageBehavior.Context>
    {
        /// <summary>
        /// 
        /// </summary>
        public class Context : TransportReceiveContext
        {
            const string MessageHandledSuccessfullyKey = "TransportReceiver.MessageHandledSuccessfully";

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parentContext"></param>
            protected Context(BehaviorContext parentContext)
                : base(parentContext)
            {

            }

            internal Context(TransportReceiveContext parentContext)
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
}