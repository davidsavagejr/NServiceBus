namespace NServiceBus.Transports
{
    using System;

    /// <summary>
    /// Interface to implement when developing custom dequeuing strategies.
    /// </summary>
    public interface IDequeueMessages : IObservable<MessageDequeued>
    {
        /// <summary>
        /// Initializes the <see cref="IDequeueMessages"/>.
        /// </summary>
        void Init(DequeueSettings settings);
        
        /// <summary>
        /// Starts the dequeuing of message/>.
        /// </summary>
        void Start();
        
        /// <summary>
        /// Stops the dequeuing of messages.
        /// </summary>
        void Stop();

        /// <summary>
        /// Changes the concurrency level of the receiver
        /// </summary>
        /// <param name="newConcurrencyLevel">The new concurrency level to use</param>
        void ChangeConcurrencyLevel(int newConcurrencyLevel);
    }

    /// <summary>
    /// TBD
    /// </summary>
    public class DequeueSettings
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="maximumConcurrencyLevel"></param>
        public DequeueSettings(string queue, int maximumConcurrencyLevel)
        {
            if (string.IsNullOrEmpty(queue))
            {
                throw new ArgumentException("Input queue must be specified");
            }

            QueueName = queue;
            MaximumConcurrencyLevel = maximumConcurrencyLevel;
        }

        /// <summary>
        /// The native queue to consume messages from
        /// </summary>
        public string QueueName{ get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int MaximumConcurrencyLevel { get; private set; }
 }

    /// <summary>
    /// 
    /// </summary>
    public struct MessageDequeued
    {
        
    }
}