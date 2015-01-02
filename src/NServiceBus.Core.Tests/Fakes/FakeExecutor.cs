namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using NServiceBus.Pipeline;

    class FakeExecutor : IExecutor
    {
        public void Dispose()
        {
        }

        public void Start(string[] pipelineIds)
        {
        }

        public void Execute(string pipelineId, Action action)
        {
            action();
        }

        public void Stop()
        {
        }
    }
}