namespace NServiceBus
{
    using System;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;

    class ChildContainerBehavior : HomomorphicBehavior<AbortableContext>
    {
        public override void DoInvoke(AbortableContext context, Action next)
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