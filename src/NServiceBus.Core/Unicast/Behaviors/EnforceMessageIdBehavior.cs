namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class EnforceMessageIdBehavior : IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
        {
            if (string.IsNullOrWhiteSpace(context.PhysicalMessage.Id))
            {     
                throw new MessageDeserializationException("Message without message id detected");    
            }

            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("EnforceMessageId", typeof(EnforceMessageIdBehavior), "Makes sure that the message pulled from the transport contains a message id")
            {
                InsertAfter("ReceiveMessage");
                InsertBefore("FirstLevelRetries");
            }
        }
    }
}