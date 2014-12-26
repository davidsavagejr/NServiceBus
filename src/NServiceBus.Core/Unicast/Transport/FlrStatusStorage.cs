namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Collections.Concurrent;

    class FlrStatusStorage
    {
        ConcurrentDictionary<string, Tuple<int, Exception>> failuresPerMessage = new ConcurrentDictionary<string, Tuple<int, Exception>>();
        CriticalError criticalError;
        readonly BusNotifications notifications;
        int maxRetries;

        public FlrStatusStorage(int maxRetries, CriticalError criticalError, BusNotifications busNotifications)
        {
            this.maxRetries = maxRetries;
            this.criticalError = criticalError;
            notifications = busNotifications;
        }

        public bool HasMaxRetriesForMessageBeenReached(string messageId)
        {
            Tuple<int, Exception> e;

            var numberOfRetries = 0;

            if (failuresPerMessage.TryGetValue(messageId, out e))
            {
                numberOfRetries = e.Item1;
            }

            return numberOfRetries <= maxRetries;
        }

        public void ClearFailuresForMessage(TransportMessage message)
        {
            var messageId = message.Id;
            Tuple<int, Exception> e;
            failuresPerMessage.TryRemove(messageId, out e);
        }

        public void IncrementFailuresForMessage(TransportMessage message, Exception e)
        {
            var item = failuresPerMessage.AddOrUpdate(message.Id, new Tuple<int, Exception>(1, e),
                (s, i) => new Tuple<int, Exception>(i.Item1 + 1, e));

            notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(item.Item1, message, e);
        }

        void TryInvokeFaultManager(TransportMessage message, Exception exception, int numberOfAttempts)
        {
            try
            {
                message.RevertToOriginalBodyIfNeeded();
                var numberOfRetries = numberOfAttempts - 1;
                message.Headers[Headers.FLRetries] = numberOfRetries.ToString();
            }
            catch (Exception ex)
            {
                criticalError.Raise(String.Format("Fault manager failed to process the failed message with id {0}", message.Id), ex);

                throw;
            }
        }
    }
}