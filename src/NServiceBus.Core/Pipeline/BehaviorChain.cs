namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using NServiceBus.Pipeline;

    class BehaviorChain<T> where T : BehaviorContext
    {
        public BehaviorChain(IEnumerable<IBehaviorInstance<T>> behaviorList, T context, PipelineExecutor pipelineExecutor, BusNotifications notifications)
        {
            context.SetChain(this);
            this.context = context;
            this.notifications = notifications;
            foreach (var behaviorInstance in behaviorList)
            {
                itemDescriptors.Enqueue(behaviorInstance);
            }

            lookupSteps = pipelineExecutor.Incoming.Concat(pipelineExecutor.Outgoing).ToDictionary(rs => rs.BehaviorType);
        }
        
        public void Invoke()
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
            
                InvokeNext(context);

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

        public void TakeSnapshot()
        {
            snapshots.Push(new Queue<IBehaviorInstance<T>>(itemDescriptors));
        }

        public void DeleteSnapshot()
        {
            itemDescriptors = new Queue<IBehaviorInstance<T>>(snapshots.Pop());
        }

        void InvokeNext(T context)
        {
            if (itemDescriptors.Count == 0)
            {
                return;
            }

            var behavior = itemDescriptors.Dequeue();
            var stepEnded = new Observable<StepEnded>();

            try
            {
                steps.OnNext(new StepStarted(lookupSteps[behavior.Type].StepId, behavior.Type, stepEnded));

                var instance = behavior.GetInstance(context.Builder);

                var duration = Stopwatch.StartNew();

                instance.Invoke(context, () =>
                {
                    duration.Stop();
                    InvokeNext(context);
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
        }

        readonly BusNotifications notifications;
        T context;
        Queue<IBehaviorInstance<T>> itemDescriptors = new Queue<IBehaviorInstance<T>>();
        Dictionary<Type, RegisterStep> lookupSteps;
        Stack<Queue<IBehaviorInstance<T>>> snapshots = new Stack<Queue<IBehaviorInstance<T>>>();
        Observable<StepStarted> steps;
    }
}