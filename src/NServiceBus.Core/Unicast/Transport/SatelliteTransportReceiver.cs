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

        public SatelliteTransportReceiver(IBuilder builder, TransactionSettings transactionSettings, IDequeueMessages receiver, IManageMessageFailures manageMessageFailures, ReadOnlySettings settings, Configure config)
            : base(transactionSettings, receiver, manageMessageFailures, settings, config)
        {
            var pipelineModifications = settings.Get<PipelineModifications>();

            var satelliteSpecificPipeline = new PipelineModifications();


            var childContainerIndex = pipelineModifications.Additions.FindIndex(s => s.StepId == WellKnownStep.CreateChildContainer);


            satelliteSpecificPipeline.Additions.AddRange(pipelineModifications.Additions.GetRange(0, childContainerIndex));
            satelliteSpecificPipeline.Additions.Add(RegisterStep.Create("ExecuteSatelliteHandler", typeof(ExecuteSatelliteHandlerBehavior), "Executes the specific satellite handler"));

            pipelineExecutor = new PipelineExecutor(builder, builder.Build<BusNotifications>(), satelliteSpecificPipeline);

        }

        PipelineExecutor pipelineExecutor;

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