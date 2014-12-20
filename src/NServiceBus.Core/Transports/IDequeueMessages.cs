namespace NServiceBus.Transports
{
    using System;
    using System.Messaging;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Interface to implement when developing custom dequeuing strategies.
    /// </summary>
    public interface IDequeueMessages : IObservable<MessageAvailable>
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
        /// <param name="purgeOnStartup"></param>
        public DequeueSettings(string queue, int maximumConcurrencyLevel,bool purgeOnStartup = false)
        {
            if (string.IsNullOrEmpty(queue))
            {
                throw new ArgumentException("Input queue must be specified");
            }
            PurgeOnStartup = purgeOnStartup;
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

        /// <summary>
        /// Tells the dequeuer if the queue should be purged before starting to consume messages from it
        /// </summary>
        public bool PurgeOnStartup { get; private set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class MessageAvailable
    {
        readonly Action<IncomingContext> contextAction;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextAction"></param>
        public MessageAvailable(Action<IncomingContext> contextAction)
        {
            this.contextAction = contextAction;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public void InitalizeContext(IncomingContext context)
        {
            contextAction(context);
        }
    }
}