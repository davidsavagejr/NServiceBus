namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Collections.Concurrent;

    class FlrStatusStorage
    {
       
        public void ClearFailuresForMessage(string messageId)
        {
            int e;
            failuresPerMessage.TryRemove(messageId, out e);
        }

        public void IncrementFailuresForMessage(string messageId, Exception e)
        {
            failuresPerMessage.AddOrUpdate(messageId,1,
                (s, i) => i + 1);
        }

        //void TryInvokeFaultManager(TransportMessage message, Exception exception, int numberOfAttempts)
        //{
        //    try
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        //criticalError.Raise(String.Format("Fault manager failed to process the failed message with id {0}", message.Id), ex);

        //        throw;
        //    }
        //}

        public int GetRetriesForMessage(string messageId)
        {
            int e;

            if (!failuresPerMessage.TryGetValue(messageId, out e))
            {
                return 0;
            }
            return e;
        }

        ConcurrentDictionary<string, int> failuresPerMessage = new ConcurrentDictionary<string, int>();
       
    }
}