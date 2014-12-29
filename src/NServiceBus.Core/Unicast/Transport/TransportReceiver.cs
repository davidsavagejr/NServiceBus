namespace NServiceBus.Unicast.Transport
{
    using System;
    using NServiceBus.Faults;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport.Monitoring;

    //Shared thread pool dispatcher
    //Individual thread pool dispatcher
    //Shared throughput limit
    //Individual throughput limit

    /// <summary>
    ///     Default implementation of a NServiceBus transport.
    /// </summary>
    public class TransportReceiver : IDisposable, IObserver<MessageAvailable>
    {
        internal TransportReceiver(string id, TransactionSettings transactionSettings, IDequeueMessages receiver, string queue, bool purgeOnStartup, PipelineExecutor pipelineExecutor, IExecutor executor, IManageMessageFailures manageMessageFailures, ReadOnlySettings settings, Configure config)
        {
            this.id = id;
            this.pipelineExecutor = pipelineExecutor;
            this.executor = executor;
            this.settings = settings;
            this.config = config;
            dequeueSettings = new DequeueSettings(queue, purgeOnStartup: purgeOnStartup);
            TransactionSettings = transactionSettings;
            FailureManager = manageMessageFailures;
            Receiver = receiver;
        }

        /// <summary>
        ///     The receiver responsible for notifying the transport when new messages are available
        /// </summary>
        public IDequeueMessages Receiver { get; set; }

        /// <summary>
        ///     Manages failed message processing.
        /// </summary>
        public IManageMessageFailures FailureManager { get; set; }

        /// <summary>
        ///     Gets the maximum concurrency level this <see cref="TransportReceiver" /> is able to support.
        /// </summary>
        public virtual int MaximumConcurrencyLevel { get; private set; }

        /// <summary>
        ///     The <see cref="TransactionSettings" /> being used.
        /// </summary>
        public TransactionSettings TransactionSettings { get; private set; }

        /// <summary>
        /// Gets the ID of this pipeline
        /// </summary>
        public string Id
        {
            get { return id; }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        void IDisposable.Dispose()
        {
            //Injected at compile time
        }

        void IObserver<MessageAvailable>.OnNext(MessageAvailable value)
        {
            try
            {
                InvokePipeline(value);
            }
            catch (Exception ex)
            {
                Logger.Error("boom", ex);
            }
            //TODO: I think I need to do some logging here, if a behavior can't be instantiated no error message is shown!
            //todo: I want to start a new instance of a pipeline and not use thread statics 
        }

        private void InvokePipeline(MessageAvailable value)
        {
            var context = new IncomingContext(pipelineExecutor.CurrentContext);

            value.InitializeContext(context);

            context.Set(dequeueSettings);
            context.Set(currentReceivePerformanceDiagnostics);
            SetContext(context);

            executor.Execute(Id, () => pipelineExecutor.InvokeReceivePhysicalMessagePipeline(context));
        }

        /// <summary>
        /// Sets the context for processing an incoming message.
        /// </summary>
        /// <param name="context"></param>
        protected virtual void SetContext(IncomingContext context)
        {
        }
        
        void IObserver<MessageAvailable>.OnError(Exception error)
        {
        }

        void IObserver<MessageAvailable>.OnCompleted()
        {
        }

        /// <summary>
        ///     Starts the transport listening for messages on the given local address.
        /// </summary>
        public virtual void Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }
            var address = Address.Parse(dequeueSettings.QueueName);

            receiveAddress = address;

            var returnAddressForFailures = address;

            var workerRunsOnThisEndpoint = settings.GetOrDefault<bool>("Worker.Enabled");

            if (workerRunsOnThisEndpoint
                && (returnAddressForFailures.Queue.ToLower().EndsWith(".worker") || address == config.LocalAddress))
                //this is a hack until we can refactor the SLR to be a feature. "Worker" is there to catch the local worker in the distributor
            {
                returnAddressForFailures = settings.Get<Address>("MasterNode.Address");

                Logger.InfoFormat("Worker started, failures will be redirected to {0}", returnAddressForFailures);
            }

            FailureManager.Init(returnAddressForFailures);

            InitializePerformanceCounters();

            Logger.DebugFormat("Pipeline {0} is starting receiver for queue {0}.", Id, dequeueSettings.QueueName);

            Receiver.Init(dequeueSettings);

            StartReceiver();

            isStarted = true;
        }

        /// <summary>
        ///     Stops the transport.
        /// </summary>
        public virtual void Stop()
        {
            InnerStop();
        }

        void InitializePerformanceCounters()
        {
            currentReceivePerformanceDiagnostics = new ReceivePerformanceDiagnostics(receiveAddress);

            currentReceivePerformanceDiagnostics.Initialize();
        }

        void StartReceiver()
        {
            Receiver.Subscribe(this);
            Receiver.Start();
        }

        /// <summary>
        /// </summary>
        protected virtual void InnerStop()
        {
            if (!isStarted)
            {
                return;
            }

            Receiver.Stop();

            isStarted = false;
        }

        void DisposeManaged()
        {
            InnerStop();

            if (currentReceivePerformanceDiagnostics != null)
            {
                currentReceivePerformanceDiagnostics.Dispose();
            }
        }

        /// <summary>
        /// </summary>
        public override string ToString()
        {
            return "Pipeline " + id;
        }

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();

        readonly Configure config;


        readonly string id;
        readonly PipelineExecutor pipelineExecutor;
        readonly IExecutor executor;
        readonly ReadOnlySettings settings;

        /// <summary>
        /// </summary>
        internal ReceivePerformanceDiagnostics currentReceivePerformanceDiagnostics;
        
        bool isStarted;
        Address receiveAddress;
        readonly DequeueSettings dequeueSettings;
    }
}