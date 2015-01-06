namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using NServiceBus.Pipeline;

    class BehaviorChain
    {
        public BehaviorChain(IEnumerable<BehaviorInstance> behaviorList, BehaviorContext context, PipelineExecutor pipelineExecutor, BusNotifications notifications)
        {
            this.context = context;
            this.notifications = notifications;

            itemDescriptors = behaviorList.ToArray();

            lookupSteps = pipelineExecutor.Incoming.Concat(pipelineExecutor.Outgoing).ToDictionary(rs => rs.BehaviorType);
        }
        
        public void Invoke(BehaviorContextStacker contextStacker)
        {
            var outerPipe = false;

            try
            {
                if (!context.TryGet("Diagnostics.Pipe", out steps))
                {
                    outerPipe = true;
                    steps = new Observable<StepStarted>();
                    context.Set("Diagnostics.Pipe", steps);
                    notifications.Pipeline.InvokeReceiveStarted(steps);
                }

                InvokeNext(context, contextStacker, 0);

                if (outerPipe)
                {
                    steps.OnCompleted();
                }
            }
            catch (Exception ex)
            {
                if (outerPipe)
                {
                    steps.OnError(ex);
                }

                throw;
            }
            finally
            {
                if (outerPipe)
                {
                    context.Remove("Diagnostics.Pipe");
                }
            }
        }

        void InvokeNext(BehaviorContext context, BehaviorContextStacker contextStacker, int currentIndex)
        {
            if (currentIndex == itemDescriptors.Length)
            {
                return;
            }

            var behavior = itemDescriptors[currentIndex];
            var stepEnded = new Observable<StepEnded>();
            contextStacker.Push(context);
            try
            {
                steps.OnNext(new StepStarted(lookupSteps[behavior.Type].StepId, behavior.Type, stepEnded));

                var instance = behavior.GetInstance(context.Builder);

                var duration = Stopwatch.StartNew();

                behavior.Invoke(instance, context, newContext =>
                {
                    duration.Stop();
                    InvokeNext(newContext, contextStacker, currentIndex + 1);
                    duration.Start();
                });

                duration.Stop();

                stepEnded.OnNext(new StepEnded(duration.Elapsed));
                stepEnded.OnCompleted();
            }
            catch (Exception ex)
            {
                stepEnded.OnError(ex);

                throw;
            }
            finally
            {
                contextStacker.Pop();
            }
        }

        readonly BusNotifications notifications;
        BehaviorContext context;
        BehaviorInstance[] itemDescriptors;
        Dictionary<Type, RegisterStep> lookupSteps;
        Observable<StepStarted> steps;
    }
}