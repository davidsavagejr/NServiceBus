namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using NServiceBus.Unicast.Transport;
    using Pipeline;
    using Pipeline.Contexts;

    class ExecuteLogicalMessagesBehavior : IBehavior<IncomingContext, IncomingLogicalMessageContext>
    {
        public void Invoke(IncomingContext context, Action<IncomingLogicalMessageContext> next)
        {
            var logicalMessages = context.LogicalMessages;

            foreach (var message in logicalMessages)
            {
                next(new IncomingLogicalMessageContext(message, context));
            }

            if (!context.PhysicalMessage.IsControlMessage())
            {
                if (!logicalMessages.Any())
                {
                    log.Warn("Received an empty message - ignoring.");
                }
            }
        }

        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }
}