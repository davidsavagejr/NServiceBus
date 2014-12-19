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
        /// <param name="address"></param>
        /// <param name="maximumConcurrencyLevel"></param>
        /// <param name="isTransactional"></param>
        public DequeueSettings(Address address, int maximumConcurrencyLevel, bool isTransactional)
        {
            Address = address;
            MaximumConcurrencyLevel = maximumConcurrencyLevel;
            IsTransactional = isTransactional;
        }

        /// <summary>
        /// 
        /// </summary>
        public Address Address { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int MaximumConcurrencyLevel { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsTransactional { get; private set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct MessageDequeued
    {
        
    }
}