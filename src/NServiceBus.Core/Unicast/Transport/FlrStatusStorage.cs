namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Collections.Concurrent;

    class FlrStatusStorage
    {
       
        public void ClearFailuresForMessage(string messageId)
        {;
            Tuple<int, Exception> e;
            failuresPerMessage.TryRemove(messageId, out e);
        }

        public void IncrementFailuresForMessage(string messageId, Exception e)
        {
            failuresPerMessage.AddOrUpdate(messageId, new Tuple<int, Exception>(1, e),
                (s, i) => new Tuple<int, Exception>(i.Item1 + 1, e));

            //notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(item.Item1, message, e);
        }

        //void TryInvokeFaultManager(TransportMessage message, Exception exception, int numberOfAttempts)
        //{
        //    try
        //    {
        //        message.RevertToOriginalBodyIfNeeded();
        //        var numberOfRetries = numberOfAttempts - 1;
        //        message.Headers[Headers.FLRetries] = numberOfRetries.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        //criticalError.Raise(String.Format("Fault manager failed to process the failed message with id {0}", message.Id), ex);

        //        throw;
        //    }
        //}

        public int GetRetriesForMessage(string messageId)
        {
            Tuple<int, Exception> e;

            if (!failuresPerMessage.TryGetValue(messageId, out e))
            {
                return 0;
            }
            return e.Item1;
        }

        ConcurrentDictionary<string, Tuple<int, Exception>> failuresPerMessage = new ConcurrentDictionary<string, Tuple<int, Exception>>();
       
    }
}