namespace NServiceBus.Unicast.Transport
{
    using NServiceBus.Faults;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Satellites.Config;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class SatelliteTransportReceiver : TransportReceiver
    {
        SatelliteContext satelliteContext;

        public SatelliteTransportReceiver(TransactionSettings transactionSettings, IDequeueMessages receiver, IManageMessageFailures manageMessageFailures, ReadOnlySettings settings, Configure config)
            : base(transactionSettings, receiver, manageMessageFailures, settings, config)
        {
        }

        PipelineExecutor pipelineExecutor;

        public void SetContext(SatelliteContext satelliteContext)
        {
            this.satelliteContext = satelliteContext;
            pipelineExecutor = satelliteContext.PipelineExecutor;
        }

        protected override void InvokePipeline(MessageAvailable value)
        {
            var context = new IncomingContext(pipelineExecutor.CurrentContext);

            value.InitializeContext(context);

            context.Set(dequeueSettings);
            context.Set(currentReceivePerformanceDiagnostics);
            context.Set(satelliteContext.Instance);

            pipelineExecutor.InvokeReceivePhysicalMessagePipeline(context);
        }
    }
}