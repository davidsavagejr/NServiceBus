namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using NServiceBus.Faults;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class MainPipelineFactory : PipelineFactory
    {
        public virtual IEnumerable<TransportReceiver> BuildPipelines(IBuilder builder, ReadOnlySettings settings, IExecutor executor)
        {
            var pipeline = new TransportReceiver(
                "Main",
                builder.Build<IDequeueMessages>(),
                settings.LocalAddress().Queue,
                settings.GetOrDefault<bool>("Transport.PurgeOnStartup"),
                builder.Build<PipelineExecutor>(),
                executor,
                builder.Build<IManageMessageFailures>(),
                settings,
                builder.Build<Configure>());
            yield return pipeline;
        }
    }
}