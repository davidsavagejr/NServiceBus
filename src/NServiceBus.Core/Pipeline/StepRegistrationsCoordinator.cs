namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;
    using NServiceBus.Pipeline.Contexts;

    class StepRegistrationsCoordinator
    {
        public StepRegistrationsCoordinator(List<RemoveStep> removals, List<ReplaceBehavior> replacements)
        {
            this.removals = removals;
            this.replacements = replacements;
        }

        public void Register(WellKnownStep wellKnownStep, Type behavior, string description)
        {
            additions.Add(RegisterStep.Create(wellKnownStep, behavior, description, false));
        }

        public void Register(RegisterStep rego)
        {
            additions.Add(rego);
        }

        public PipelineRuntimeModel BuildRuntimeModel()
        {
            var registrations = CreateRegistrationsList().ToList();

            var incomingPipelineSteps = registrations.Where(x => typeof(IncomingContext).IsAssignableFrom(GetInputType(x.BehaviorType))).ToList();
            var outgoingPipelineSteps = registrations.Where(x => typeof(OutgoingContext).IsAssignableFrom(GetInputType(x.BehaviorType))).ToList();

            return new PipelineRuntimeModel(
                Sort(typeof(IncomingContext), incomingPipelineSteps), 
                Sort(typeof(OutgoingContext), outgoingPipelineSteps));
        }

        IEnumerable<RegisterStep> CreateRegistrationsList()
        {
            var registrations = new Dictionary<string, RegisterStep>(StringComparer.CurrentCultureIgnoreCase);
            var listOfBeforeAndAfterIds = new List<string>();

            // Let's do some validation too

            //Step 1: validate that additions are unique
            foreach (var metadata in additions)
            {
                if (!registrations.ContainsKey(metadata.StepId))
                {
                    registrations.Add(metadata.StepId, metadata);
                    if (metadata.Afters != null)
                    {
                        listOfBeforeAndAfterIds.AddRange(metadata.Afters.Select(a => a.Id));
                    }
                    if (metadata.Befores != null)
                    {
                        listOfBeforeAndAfterIds.AddRange(metadata.Befores.Select(b => b.Id));
                    }

                    continue;
                }

                var message = string.Format("Step registration with id '{0}' is already registered for '{1}'", metadata.StepId, registrations[metadata.StepId].BehaviorType);
                throw new Exception(message);
            }

            //  Step 2: do replacements
            foreach (var metadata in replacements)
            {
                if (!registrations.ContainsKey(metadata.ReplaceId))
                {
                    var message = string.Format("You can only replace an existing step registration, '{0}' registration does not exist!", metadata.ReplaceId);
                    throw new Exception(message);
                }

                registrations[metadata.ReplaceId].BehaviorType = metadata.BehaviorType;
                if (!String.IsNullOrEmpty(metadata.Description))
                {
                    registrations[metadata.ReplaceId].Description = metadata.Description;
                }
            }

            // Step 3: validate the removals
            foreach (var metadata in removals.Distinct(new CaseInsensitiveIdComparer()))
            {
                if (!registrations.ContainsKey(metadata.RemoveId))
                {
                    var message = string.Format("You cannot remove step registration with id '{0}', registration does not exist!", metadata.RemoveId);
                    throw new Exception(message);
                }

                if (listOfBeforeAndAfterIds.Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase))
                {
                    var add = additions.First(mr => (mr.Befores != null && mr.Befores.Select(b => b.Id).Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)) ||
                                                    (mr.Afters != null && mr.Afters.Select(b => b.Id).Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)));

                    var message = string.Format("You cannot remove step registration with id '{0}', registration with id {1} depends on it!", metadata.RemoveId, add.StepId);
                    throw new Exception(message);
                }

                registrations.Remove(metadata.RemoveId);
            }

            

            return registrations.Values;
        }

        static IEnumerable<RegisterStep> Sort(Type beginContext, IList<RegisterStep> registrations)
        {
            // Step 0: Add dependencies on pipeline and stage begins
            var pipelineBeginNode = new Node("PipelineBegin", new Dependency[0]);
            var stageBeginMap = registrations
               .Select(x => new
               {
                   r = x,
                   t = x.BehaviorType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStartStage<>))
               })
               .Where(x => x.t != null)
               .ToDictionary(x => x.t.GetGenericArguments()[0], x => x.r.StepId);

            stageBeginMap[beginContext] = pipelineBeginNode.StepId;

            foreach (var registration in registrations)
            {
                var inputType = GetInputType(registration.BehaviorType);
                var stageBegin = stageBeginMap[inputType];
                registration.InsertAfter(stageBegin);
            }

            // Step 1: create nodes for graph
            var nameToNodeDict = new Dictionary<string, Node>();
            var allNodes = new List<Node>();
            foreach (var rego in registrations)
            {
                // create entries to preserve order within
                var node = new Node(rego);
                nameToNodeDict[rego.StepId] = node;
                allNodes.Add(node);
            }

            allNodes.Add(pipelineBeginNode);
            nameToNodeDict[pipelineBeginNode.StepId] = pipelineBeginNode;

            // Step 2: create edges from InsertBefore/InsertAfter values
            foreach (var node in allNodes)
            {
                if (node.Befores != null)
                {
                    foreach (var beforeReference in node.Befores)
                    {
                        Node referencedNode;
                        if (nameToNodeDict.TryGetValue(beforeReference.Id, out referencedNode))
                        {
                            referencedNode.previous.Add(node);
                        }
                        else
                        {
                            var message = string.Format("Registration '{0}' specified in the insertbefore of the '{1}' step does not exist!", beforeReference.Id, node.StepId);

                            if (!beforeReference.Enforce)
                            {
                                Logger.Info(message);
                            }
                            else
                            {
                                throw new Exception(message);
                            }
                        }
                    }
                }

                if (node.Afters != null)
                {
                    foreach (var afterReference in node.Afters)
                    {
                        Node referencedNode;
                        if (nameToNodeDict.TryGetValue(afterReference.Id, out referencedNode))
                        {
                            node.previous.Add(referencedNode);
                        }
                        else
                        {
                            var message = string.Format("Registration '{0}' specified in the insertafter of the '{1}' step does not exist!", afterReference.Id, node.StepId);

                            if (!afterReference.Enforce)
                            {
                                Logger.Info(message);
                            }
                            else
                            {
                                throw new Exception(message);
                            }
                        }
                    }
                }
            }

            // Step 3: Perform Topological Sort
            var output = new List<RegisterStep>();
            foreach (var node in allNodes)
            {
                node.Visit(output);
            }

            // Step 4: Validate intput and output types
            for (var i = 1; i < output.Count; i++)
            {
                var previousBehavior = output[i - 1].BehaviorType;
                var thisBehavior = output[i].BehaviorType;

                var incomingType = GetOutputType(previousBehavior);
                var inputType = GetInputType(thisBehavior);

                //There is no connection between the incoming and outgoing pipes.
                if (incomingType != typeof(OutgoingContext) && inputType == typeof(OutgoingContext))
                {
                    continue;
                }

                if (!inputType.IsAssignableFrom(incomingType))
                {
                    throw new Exception(string.Format("Cannot chain behavior {0} and {1} together because output type of behvaior {0} ({2}) cannot be passed as input for behavior {1} ({3})",
                        previousBehavior.FullName,
                        thisBehavior.FullName,
                        incomingType,
                        inputType));
                }
            }
            return output;
        }

        static Type GetOutputType(Type behaviorType)
        {
            var behaviorInterface = GetBehaviorInterface(behaviorType);
            return behaviorInterface.GetGenericArguments()[1];
        }

        static Type GetBehaviorInterface(Type behaviorType)
        {
            var behaviorInterface = behaviorType.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBehavior<,>));
            return behaviorInterface;
        }

        static bool IsConnectorTo(Type candidateBehavior, Type inputType)
        {
            var output = GetOutputType(candidateBehavior);
            if (output != inputType)
            {
                return false;
            }
            var input = GetInputType(candidateBehavior);
            return typeof(StageConnector<,>).MakeGenericType(input, output).IsAssignableFrom(candidateBehavior);
        }

        static Type GetInputType(Type behaviorType)
        {
            var behaviorInterface = GetBehaviorInterface(behaviorType);
            return behaviorInterface.GetGenericArguments()[0];
        }

        List<RegisterStep> additions = new List<RegisterStep>();
        List<RemoveStep> removals;
        List<ReplaceBehavior> replacements;

        static ILog Logger = LogManager.GetLogger<StepRegistrationsCoordinator>();

        class CaseInsensitiveIdComparer : IEqualityComparer<RemoveStep>
        {
            public bool Equals(RemoveStep x, RemoveStep y)
            {
                return x.RemoveId.Equals(y.RemoveId, StringComparison.CurrentCultureIgnoreCase);
            }

            public int GetHashCode(RemoveStep obj)
            {
                return obj.RemoveId.ToLower().GetHashCode();
            }
        }

        class Node
        {
            internal void Visit(ICollection<RegisterStep> output)
            {
                if (visited)
                {
                    return;
                }
                visited = true;
                foreach (var n in previous)
                {
                    n.Visit(output);
                }
                if (rego != null)
                {
                    output.Add(rego);
                }
            }

            public Node(string id, IList<Dependency> befores)
            {
                StepId = id;
                Befores = befores;
                Afters = new Dependency[] { };
            }

            public Node(RegisterStep registerStep)
            {
                rego = registerStep;
                Befores = registerStep.Befores;
                Afters = registerStep.Afters;
                StepId = registerStep.StepId;
            }

            public readonly string StepId;
            private readonly RegisterStep rego;
            public readonly IList<Dependency> Befores;
            public readonly IList<Dependency> Afters;
            internal List<Node> previous = new List<Node>();
            bool visited;
        }

        public void Register(string pipelineStep, Type behavior, string description)
        {
            Register(WellKnownStep.Create(pipelineStep), behavior, description);
        }
    }
}