namespace NServiceBus.Pipeline.Contexts
{
    /// <summary>
    /// The first context in the behavior chain
    /// </summary>
    public class BootstrapContext : BehaviorContext
    {
        /// <summary>
        /// The first context in the behavior chain
        /// </summary>
        /// <param name="parentContext"></param>
        public BootstrapContext(BehaviorContext parentContext) 
            : base(parentContext)
        {
        }
    }
}