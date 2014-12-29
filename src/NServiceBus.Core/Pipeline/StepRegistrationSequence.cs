namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// </summary>
    public class StepRegistrationSequence
    {
        string previousStep;
        readonly Action<RegisterStep> addStep;

        internal StepRegistrationSequence(Action<RegisterStep> addStep, string previousStep)
        {
            this.addStep = addStep;
            this.previousStep = previousStep;
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="behavior">The <see cref="IBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public StepRegistrationSequence Register(string stepId, Type behavior, string description)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");

            if (string.IsNullOrEmpty(stepId))
            {
                throw new ArgumentNullException("stepId");
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description");
            }

            var step = RegisterStep.Create(stepId, behavior, description);
            step.InsertAfter(previousStep);
            addStep(step);
            previousStep = stepId;
            return this;
        }


        /// <summary>
        /// <see cref="Register(string,System.Type,string)"/>
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the step to add.</param>
        /// <param name="behavior">The <see cref="IBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public StepRegistrationSequence Register(WellKnownStep wellKnownStep, Type behavior, string description)
        {
            if (wellKnownStep == null)
            {
                throw new ArgumentNullException("wellKnownStep");
            }

            Register((string)wellKnownStep, behavior, description);
            return this;
        }
    }
}