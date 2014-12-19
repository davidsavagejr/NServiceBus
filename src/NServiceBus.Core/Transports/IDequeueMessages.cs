namespace NServiceBus.Transports
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using Unicast.Transport;

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
        public Address Address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TransactionSettings TransactionSettings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int MaximumConcurrencyLevel { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct MessageDequeued
    {
        
    }
}