namespace NServiceBus.Faults.Forwarder
{
    using System;
    using NServiceBus.Hosting;
    using NServiceBus.Logging;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Queuing;

    class ForwardingFaultManager : IManageMessageFailures
    {
        public ForwardingFaultManager(ISendMessages sender, string errorQueue, HostInformation hostInformation, BusNotifications busNotifications)
        {
            this.sender = sender;
            this.errorQueue = errorQueue;
            this.hostInformation = hostInformation;
            this.busNotifications = busNotifications;
        }

        void IManageMessageFailures.SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            TryHandleFailure(() => SendToErrorQueue(message, e));
        }

        void IManageMessageFailures.ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            TryHandleFailure(() => SendToErrorQueue(message, e));
        }

        void IManageMessageFailures.Init(Address address)
        {
            localAddress = address;
        }

        void SendToErrorQueue(TransportMessage message, Exception exception)
        {

            message.SetExceptionHeaders(exception, localAddress);

            message.Headers.Remove(Headers.Retries);


            message.Headers[Headers.HostId] = hostInformation.HostId.ToString("N");
            message.Headers[Headers.HostDisplayName] = hostInformation.DisplayName;


            Logger.ErrorFormat("Message {0} will be moved to the configured error queue {1}", message.Id, errorQueue);

            sender.Send(message, new SendOptions(Address.Parse(errorQueue)));
            busNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message, exception);
        }

        void TryHandleFailure(Action failureHandlingAction)
        {
            try
            {
                failureHandlingAction();
            }
            catch (QueueNotFoundException exception)
            {
                var errorMessage = string.Format("Could not forward failed message to error queue '{0}' as it could not be found.", exception.Queue);
                Logger.Fatal(errorMessage);
                throw new InvalidOperationException(errorMessage, exception);
            }
            catch (Exception exception)
            {
                var errorMessage = "Could not forward failed message to error queue.";
                Logger.Fatal(errorMessage, exception);
                throw new InvalidOperationException(errorMessage, exception);
            }
        }

      
        static ILog Logger = LogManager.GetLogger<ForwardingFaultManager>();
     
        readonly BusNotifications busNotifications;
        readonly ISendMessages sender;
        readonly string errorQueue;
        readonly HostInformation hostInformation;
        Address localAddress;
    }
}