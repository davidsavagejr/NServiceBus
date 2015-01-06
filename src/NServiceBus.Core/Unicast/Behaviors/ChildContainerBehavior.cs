namespace NServiceBus
{
    using System;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;

    class ChildContainerBehavior : HomomorphicBehavior<IncomingContext>
    {
        public override void DoInvoke(IncomingContext context, Action next)
        {
            using (var childBuilder = context.Builder.CreateChildBuilder())
            {
                context.Set(childBuilder);
                try
                {
                    next();
                }
                finally
                {
                    context.Remove<IBuilder>();
                }
            }
        }
    }
}