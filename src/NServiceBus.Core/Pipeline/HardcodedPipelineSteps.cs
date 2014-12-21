namespace NServiceBus.Pipeline
{
    using NServiceBus.Logging;
    using NServiceBus.Unicast.Transport;

    class HardcodedPipelineSteps
    {
        public static void Register(PipelineSettings pipeline)
        {
            RegisterIncomingCoreBehaviors(pipeline);

            RegisterOutgoingCoreBehaviors(pipeline);
        }

        static void RegisterIncomingCoreBehaviors(PipelineSettings pipeline)
        {
            pipeline.Register("ReceivePerformanceDiagnosticsBehavior", typeof(ReceivePerformanceDiagnosticsBehavior), "Add ProcessingStarted and ProcessingEnded headers");
            pipeline.Register(WellKnownStep.ProcessingStatistics, typeof(ProcessingStatisticsBehavior), "Add ProcessingStarted and ProcessingEnded headers");
            pipeline.Register(WellKnownStep.CreateChildContainer, typeof(ChildContainerBehavior), "Creates the child container");
            pipeline.Register(WellKnownStep.ExecuteUnitOfWork, typeof(UnitOfWorkBehavior), "Executes the UoW");
            pipeline.Register("ProcessSubscriptionRequests", typeof(SubscriptionReceiverBehavior), "Check for subscription messages and execute the requested behavior to subscribe or unsubscribe.");
            pipeline.Register(WellKnownStep.MutateIncomingTransportMessage, typeof(ApplyIncomingTransportMessageMutatorsBehavior), "Executes IMutateIncomingTransportMessages");
            pipeline.Register(WellKnownStep.DeserializeMessages, typeof(DeserializeLogicalMessagesBehavior), "Deserializes the physical message body into logical messages");
            pipeline.Register("InvokeRegisteredCallbacks", typeof(CallbackInvocationBehavior), "Updates the callback inmemory dictionary");
            pipeline.Register(WellKnownStep.ExecuteLogicalMessages, typeof(ExecuteLogicalMessagesBehavior), "Starts the execution of each logical message");
            pipeline.Register(WellKnownStep.MutateIncomingMessages, typeof(ApplyIncomingMessageMutatorsBehavior), "Executes IMutateIncomingMessages");
            pipeline.Register(WellKnownStep.LoadHandlers, typeof(LoadHandlersBehavior), "Gets all the handlers to invoke from the MessageHandler registry based on the message type.");
            pipeline.Register("SetCurrentMessageBeingHandled", typeof(SetCurrentMessageBeingHandledBehavior), "Sets the static current message (this is used by the headers)");
            pipeline.Register(WellKnownStep.InvokeHandlers, typeof(InvokeHandlersBehavior), "Calls the IHandleMessages<T>.Handle(T)");
        }

        static void RegisterOutgoingCoreBehaviors(PipelineSettings pipeline)
        {
            pipeline.Register(WellKnownStep.EnforceBestPractices, typeof(SendValidatorBehavior), "Enforces messaging best practices");
            pipeline.Register(WellKnownStep.MutateOutgoingMessages, typeof(MutateOutgoingMessageBehavior), "Executes IMutateOutgoingMessages");
            pipeline.Register("PopulateAutoCorrelationHeadersForReplies", typeof(PopulateAutoCorrelationHeadersForRepliesBehavior), "Copies existing saga headers from incoming message to outgoing message to facilitate the auto correlation in the saga, when replying to a message that was sent by a saga.");
            pipeline.Register(WellKnownStep.CreatePhysicalMessage, typeof(CreatePhysicalMessageBehavior), "Converts a logical message into a physical message");
            pipeline.Register(WellKnownStep.SerializeMessage, typeof(SerializeMessagesBehavior), "Serializes the message to be sent out on the wire");
            pipeline.Register(WellKnownStep.MutateOutgoingTransportMessage, typeof(MutateOutgoingPhysicalMessageBehavior), "Executes IMutateOutgoingTransportMessages");
            if (LogManager.GetLogger("LogOutgoingMessage").IsDebugEnabled)
            {
                pipeline.Register("LogOutgoingMessage", typeof(LogOutgoingMessageBehavior), "Logs the message contents before it is sent.");
            }
            pipeline.Register(WellKnownStep.DispatchMessageToTransport, typeof(DispatchMessageToTransportBehavior), "Dispatches messages to the transport");
        }
    }
}