namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    internal class AbortableBehavior : IBehavior<PhysicalMessageContext, AbortableContext>
    {
        public void Invoke(PhysicalMessageContext context, Action<AbortableContext> next)
        {
            var abortableContext = new AbortableContext(context);
            next(abortableContext);
            if (!abortableContext.MessageHandledSuccessfully)
            {
                throw new MessageProcessingAbortedException();
            }
        }
    }

    /// <summary>
    /// Informs that message processing has been aborted.
    /// </summary>
    [Serializable]
    public class MessageProcessingAbortedException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public MessageProcessingAbortedException()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MessageProcessingAbortedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}