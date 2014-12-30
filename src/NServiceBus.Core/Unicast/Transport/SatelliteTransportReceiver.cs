namespace NServiceBus.Unicast.Transport
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Satellites;
    using NServiceBus.Transports;

    class SatelliteTransportReceiver : TransportReceiver
    {
        ISatellite satellite;


        public SatelliteTransportReceiver(string id, IDequeueMessages receiver, string queue, bool purgeOnStartup, PipelineExecutor pipelineExecutor, IExecutor executor, ISatellite satellite) : base(id, receiver, queue, purgeOnStartup, pipelineExecutor, executor)
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