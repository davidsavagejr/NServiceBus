namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class EnforceMessageIdBehavior : HomomorphicBehavior<AbortableContext>
    {
        public override void DoInvoke(AbortableContext context, Action next)
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
                InsertAfter("AbortableBehavior");
                InsertBeforeIfExists("FirstLevelRetries");
            }
        }
    }
}