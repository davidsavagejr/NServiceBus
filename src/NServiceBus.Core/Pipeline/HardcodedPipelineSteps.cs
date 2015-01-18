namespace NServiceBus.Pipeline
{
    using NServiceBus.Logging;

    class HardcodedPipelineSteps
    {
        public static void Register(PipelineSettings pipeline, bool isSendOnly)
        {
            RegisterIncomingCoreBehaviors(pipeline, isSendOnly);
            RegisterOutgoingCoreBehaviors(pipeline);
        }

        static void RegisterIncomingCoreBehaviors(PipelineSettings pipeline, bool isSendOnly)
        {
            if (isSendOnly)
            {
                return;
            }
            pipeline
                .After(WellKnownStep.Receive)
                .Register("AbortableBehavior", typeof(TransportReceiveToPhysicalMessageProcessingConnector), "Allows to abort processing the message")
                .Register("ReceivePerformanceDiagnosticsBehavior", typeof(ReceivePerformanceDiagnosticsBehavior), "Add ProcessingStarted and ProcessingEnded headers")
                .Register(WellKnownStep.ProcessingStatistics, typeof(ProcessingStatisticsBehavior), "Add ProcessingStarted and ProcessingEnded headers")
                .Register(WellKnownStep.CreateChildContainer, typeof(ChildContainerBehavior), "Creates the child container")
                .Register(WellKnownStep.ExecuteUnitOfWork, typeof(UnitOfWorkBehavior), "Executes the UoW")
                .Register("ProcessSubscriptionRequests", typeof(SubscriptionReceiverBehavior), "Check for subscription messages and execute the requested behavior to subscribe or unsubscribe.")
                .Register(WellKnownStep.MutateIncomingTransportMessage, typeof(ApplyIncomingTransportMessageMutatorsBehavior), "Executes IMutateIncomingTransportMessages")
                .Register(WellKnownStep.DeserializeMessages, typeof(DeserializeLogicalMessagesBehavior), "Deserializes the physical message body into logical messages")
                .Register("InvokeRegisteredCallbacks", typeof(CallbackInvocationBehavior), "Updates the callback inmemory dictionary")
                .Register(WellKnownStep.ExecuteLogicalMessages, typeof(ExecuteLogicalMessagesBehavior), "Starts the execution of each logical message")
                .Register(WellKnownStep.MutateIncomingMessages, typeof(ApplyIncomingMessageMutatorsBehavior), "Executes IMutateIncomingMessages")
                .Register(WellKnownStep.LoadHandlers, typeof(LoadHandlersBehavior), "Gets all the handlers to invoke from the MessageHandler registry based on the message type.")
                .Register("SetCurrentMessageBeingHandled", typeof(SetCurrentMessageBeingHandledBehavior), "Sets the static current message (this is used by the headers)")
                .Register(WellKnownStep.InvokeHandlers, typeof(InvokeHandlersBehavior), "Calls the IHandleMessages<T>.Handle(T)");
        }

        static void RegisterOutgoingCoreBehaviors(PipelineSettings pipeline)
        {
            var seq = pipeline.Register(WellKnownStep.EnforceBestPractices, typeof(SendValidatorBehavior), "Enforces messaging best practices")
                .Register(WellKnownStep.MutateOutgoingMessages, typeof(MutateOutgoingMessageBehavior), "Executes IMutateOutgoingMessages")
                .Register("PopulateAutoCorrelationHeadersForReplies", typeof(PopulateAutoCorrelationHeadersForRepliesBehavior), "Copies existing saga headers from incoming message to outgoing message to facilitate the auto correlation in the saga, when replying to a message that was sent by a saga.")
                .Register(WellKnownStep.CreatePhysicalMessage, typeof(CreatePhysicalMessageBehavior), "Converts a logical message into a physical message")
                .Register(WellKnownStep.SerializeMessage, typeof(SerializeMessagesBehavior), "Serializes the message to be sent out on the wire")
                .Register(WellKnownStep.MutateOutgoingTransportMessage, typeof(MutateOutgoingPhysicalMessageBehavior), "Executes IMutateOutgoingTransportMessages");
            if (LogManager.GetLogger("LogOutgoingMessage").IsDebugEnabled)
            {
                seq = seq.Register("LogOutgoingMessage", typeof(LogOutgoingMessageBehavior), "Logs the message contents before it is sent.");
            }
            seq.Register(WellKnownStep.DispatchMessageToTransport, typeof(DispatchMessageToTransportBehavior), "Dispatches messages to the transport");
        }
    }
}