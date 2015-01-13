namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using Hosting;
    using Licensing;
    using Logging;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Routing;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;
    using Settings;

    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    public partial class UnicastBus : IStartableBus, IInMemoryOperations, IManageMessageHeaders
    {
        HostInformation hostInformation;

        /// <summary>
        /// Initializes a new instance of <see cref="UnicastBus"/>.
        /// </summary>
        public UnicastBus(
            IExecutor executor,
            CriticalError criticalError,
            IEnumerable<PipelineFactory> pipelineFactories,
            IMessageMapper messageMapper, 
            IBuilder builder, 
            Configure configure, 
            IManageSubscriptions subscriptionManager, 
            MessageMetadataRegistry messageMetadataRegistry,
            ReadOnlySettings settings,
            TransportDefinition transportDefinition,
            ISendMessages messageSender,
            StaticMessageRouter messageRouter,
            StaticOutgoingMessageHeaders outgoingMessageHeaders,
            CallbackMessageLookup callbackMessageLookup,
            PipelineExecutor pipelineExecutor)
        {
            this.executor = executor;
            this.criticalError = criticalError;
            this.pipelineFactories = pipelineFactories;
            this.settings = settings;
            this.builder = builder;
            this.pipelineExecutor = pipelineExecutor;

            var rootContext = new RootContext(builder);
            busImpl = new ContextualBus(rootContext, 
                messageMapper, 
                builder, 
                configure,
                subscriptionManager, 
                new LogicalMessageFactory(messageMetadataRegistry, messageMapper, rootContext),
                settings,
                transportDefinition,
                messageSender,
                messageRouter,
                outgoingMessageHeaders,
                callbackMessageLookup, pipelineExecutor);
        }

        /// <summary>
        /// Provides access to the current host information
        /// </summary>
        [ObsoleteEx(Message = "We have introduced a more explicit API to set the host identifier, see busConfiguration.UniquelyIdentifyRunningInstance()", TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
        public HostInformation HostInformation
        {
            get { return hostInformation; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                hostInformation = value;
            }
        }

        

        /// <summary>
        /// <see cref="IStartableBus.Start()"/>
        /// </summary>
        public IBus Start()
        {
            LicenseManager.PromptUserForLicenseIfTrialHasExpired();

            if (started)
            {
                return this;
            }

            lock (startLocker)
            {
                if (started)
                {
                    return this;
                }

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                var pipelines = pipelineFactories.SelectMany(x => x.BuildPipelines(Builder, Settings, executor)).ToArray();
                executor.Start(pipelines.Select(x => x.Id).ToArray());

                pipelineCollection = new PipelineCollection(pipelines);
                pipelineCollection.Start();

                started = true;
            }

            ProcessStartupItems(
                Builder.BuildAll<IWantToRunWhenBusStartsAndStops>().ToList(),
                toRun =>
                {
                    toRun.Start();
                    thingsRanAtStartup.Add(toRun);
                    Log.DebugFormat("Started {0}.", toRun.GetType().AssemblyQualifiedName);
                },
                ex => criticalError.Raise("Startup task failed to complete.", ex),
                startCompletedEvent);

            return this;
        }

        void ExecuteIWantToRunAtStartupStopMethods()
        {
            Log.Debug("Ensuring IWantToRunWhenBusStartsAndStops.Start has been called.");
            startCompletedEvent.WaitOne();
            Log.Debug("All IWantToRunWhenBusStartsAndStops.Start have completed now.");

            var tasksToStop = Interlocked.Exchange(ref thingsRanAtStartup, new ConcurrentBag<IWantToRunWhenBusStartsAndStops>());
            if (!tasksToStop.Any())
            {
                return;
            }

            ProcessStartupItems(
                tasksToStop,
                toRun =>
                {
                    toRun.Stop();
                    Log.DebugFormat("Stopped {0}.", toRun.GetType().AssemblyQualifiedName);
                },
                ex => Log.Fatal("Startup task failed to stop.", ex),
                stopCompletedEvent);

            stopCompletedEvent.WaitOne();
        }

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>
        /// </summary>
        public void Dispose()
        {
            //Injected at compile time
        }

// ReSharper disable once UnusedMember.Local
        void DisposeManaged()
        {
            InnerShutdown();
            busImpl.Dispose();
            Builder.Dispose();
        }

        void InnerShutdown()
        {
            if (!started)
            {
                return;
            }

            Log.Info("Initiating shutdown.");
            pipelineCollection.Stop();
            executor.Stop();
            ExecuteIWantToRunAtStartupStopMethods();

            Log.Info("Shutdown complete.");

            started = false;
        }


        volatile bool started;
        object startLocker = new object();

        static ILog Log = LogManager.GetLogger<UnicastBus>();

        ConcurrentBag<IWantToRunWhenBusStartsAndStops> thingsRanAtStartup = new ConcurrentBag<IWantToRunWhenBusStartsAndStops>();
        ManualResetEvent startCompletedEvent = new ManualResetEvent(false);
        ManualResetEvent stopCompletedEvent = new ManualResetEvent(true);

        PipelineCollection pipelineCollection;
        ContextualBus busImpl;
        ReadOnlySettings settings;
        IEnumerable<PipelineFactory> pipelineFactories;
        IExecutor executor;
        CriticalError criticalError;
        IBuilder builder;
        // ReSharper disable once NotAccessedField.Local
        PipelineExecutor pipelineExecutor;

        static void ProcessStartupItems<T>(IEnumerable<T> items, Action<T> iteration, Action<Exception> inCaseOfFault, EventWaitHandle eventToSet)
        {
            eventToSet.Reset();

            Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(items, iteration);
                eventToSet.Set();
            }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness)
            .ContinueWith(task =>
            {
                eventToSet.Set();
                inCaseOfFault(task.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.LongRunning);
        }

        /// <summary>
        /// <see cref="IManageMessageHeaders.SetHeaderAction"/>
        /// </summary>
        public Action<object, string, string> SetHeaderAction { get { return busImpl.SetHeaderAction; } }
        /// <summary>
        /// <see cref="IManageMessageHeaders.GetHeaderAction"/>
        /// </summary>
        public Func<object, string, string> GetHeaderAction { get { return busImpl.GetHeaderAction; } }

        /// <summary>
        /// Only for tests
        /// </summary>
        public ReadOnlySettings Settings
        {
            get { return settings; }
        }

        /// <summary>
        /// Only for tests
        /// </summary>
        public IBuilder Builder
        {
            get { return builder; }
        }
    }
}
