namespace NServiceBus
{
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Extension on the incoming context
    /// </summary>
    public static class IncomingContextExtensions
    {
        /// <summary>
        /// True if the message was handled successfully and the MQ operations should be committed
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool MessageHandledSuccessfully(this IncomingContext context)
        {
            bool messageHandledSuccessfully;

            if (!context.TryGet("TransportReceiver.MessageHandledSuccessfully", out messageHandledSuccessfully))
            {
                return true;
            }

            return messageHandledSuccessfully;
        }


        /// <summary>
        /// Tells the transport to rollback the current receive operation
        /// </summary>
        /// <param name="context"></param>
        public static void AbortReceiveOperation(this IncomingContext context)
        {
            context.Set("TransportReceiver.MessageHandledSuccessfully", false);
        }

        /// <summary>
        /// Registers the public receive address in the context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="address">The native transport address that should be used to address this receiver</param>
        public static void SetPublicReceiveAddress(this IncomingContext context, string address)
        {
            context.Set("Transport.PublicReceiveAddress", address);
        }

        /// <summary>
        /// Gets the public receive address
        /// </summary>
        /// <param name="context"></param>
        public static string PublicReceiveAddress(this IncomingContext context)
        {
            return context.Get<string>("Transport.PublicReceiveAddress");
        }
    }
}