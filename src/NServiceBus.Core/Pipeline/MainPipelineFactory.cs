namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using NServiceBus.Faults;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;
    using TransactionSettings = NServiceBus.Unicast.Transport.TransactionSettings;

    class MainPipelineFactory : PipelineFactory
    {
        public virtual IEnumerable<TransportReceiver> BuildPipelines(IBuilder builder, ReadOnlySettings settings, IExecutor executor)
        {
            var transactionSettings = new TransactionSettings(settings);
            var pipeline = new TransportReceiver(
                "Main",
                transactionSettings,
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