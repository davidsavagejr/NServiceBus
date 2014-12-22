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

        public SatelliteTransportReceiver(IBuilder builder, TransactionSettings transactionSettings, IDequeueMessages receiver, IManageMessageFailures manageMessageFailures, ReadOnlySettings settings, Configure config, PipelineExecutor pipelineExecutor)
            : base(transactionSettings, receiver, manageMessageFailures, settings, config, pipelineExecutor)
        {
            var pipelineModifications = settings.Get<PipelineModifications>();

            // we need to clone since multiple satellites will modify the same collections if not
            var satelliteSpecificPipeline = new PipelineModifications();

            satelliteSpecificPipeline.Additions.AddRange(pipelineModifications.Additions);

            satelliteSpecificPipeline.Removals.AddRange(pipelineModifications.Removals);

            satelliteSpecificPipeline.Replacements.AddRange(pipelineModifications.Replacements);

            satelliteSpecificPipeline.Replacements.Add(new ReplaceBehavior(WellKnownStep.CreateChildContainer, typeof(ExecuteSatelliteHandlerBehavior)));
            base.pipelineExecutor = new PipelineExecutor(builder, builder.Build<BusNotifications>(), satelliteSpecificPipeline);
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