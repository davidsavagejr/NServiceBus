namespace NServiceBus.Unicast.Transport
{
    using NServiceBus.Faults;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Satellites;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class SatelliteTransportReceiver : TransportReceiver
    {
        ISatellite satellite;


        public SatelliteTransportReceiver(string id, IDequeueMessages receiver, string queue, bool purgeOnStartup, PipelineExecutor pipelineExecutor, IExecutor executor, IManageMessageFailures manageMessageFailures, ReadOnlySettings settings, Configure config, ISatellite satellite) : base(id, receiver, queue, purgeOnStartup, pipelineExecutor, executor, manageMessageFailures, settings, config)
        {
            this.satellite = satellite;
        }

        protected override void SetContext(IncomingContext context)
        {
            base.SetContext(context);
            context.Set(satellite);
        }

        public override void Start()
        {
            base.Start();
            satellite.Start();
        }

        public override void Stop()
        {
            base.Stop();
            satellite.Stop();
        }
    }
}