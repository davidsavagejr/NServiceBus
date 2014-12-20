namespace NServiceBus.Unicast.Transport
{
    using NServiceBus.Faults;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Satellites;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class SatelliteTransportReceiver : TransportReceiver
    {
        ISatellite satellite;

        public SatelliteTransportReceiver(IBuilder builder, TransactionSettings transactionSettings, DequeueSettings dequeueSettings, IDequeueMessages receiver, IManageMessageFailures manageMessageFailures, ReadOnlySettings settings, Configure config, PipelineExecutor pipelineExecutor)
            : base(transactionSettings, dequeueSettings, receiver, manageMessageFailures, settings, config, pipelineExecutor)
        {
            var pipelineModifications = settings.Get<PipelineModifications>();
            pipelineModifications.Replacements.Add(new ReplaceBehavior(WellKnownStep.CreateChildContainer, typeof(ExecuteSatelliteHandlerBehavior)));
            base.pipelineExecutor = new PipelineExecutor(builder, builder.Build<BusNotifications>(), pipelineModifications);
        }

        public void SetSatellite(ISatellite satellite)
        {
            this.satellite = satellite;
        }

        protected override void InvokePipeline(MessageAvailable value)
        {
            var context = new IncomingContext(pipelineExecutor.CurrentContext);

            value.InitalizeContext(context);

            context.Set(firstLevelRetries);
            context.Set(currentReceivePerformanceDiagnostics);
            context.Set(satellite);

            pipelineExecutor.InvokeReceivePhysicalMessagePipeline(context);
        }
    }
}