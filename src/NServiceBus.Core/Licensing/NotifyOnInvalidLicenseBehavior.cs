namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;

    class NotifyOnInvalidLicenseBehavior : HomomorphicBehavior<IncomingContext>
    {
        public override void DoInvoke(IncomingContext context, Action next)
        {
            context.PhysicalMessage.Headers[Headers.HasLicenseExpired] = true.ToString().ToLower();

            next();

            if (Debugger.IsAttached)
            {
                Log.Error("Your license has expired");
            }
        }

        static ILog Log = LogManager.GetLogger<NotifyOnInvalidLicenseBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("LicenseReminder", typeof(NotifyOnInvalidLicenseBehavior), "Enforces the licensing policy")
            {
                InsertBefore(WellKnownStep.AuditProcessedMessage);
            }
        }
    }
}