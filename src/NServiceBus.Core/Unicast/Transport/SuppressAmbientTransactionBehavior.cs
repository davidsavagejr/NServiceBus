namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Transactions;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class SuppressAmbientTransactionBehavior : IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
        {
            if (Transaction.Current == null)
            {
                next();
                return;
            }

            using (var tx = new TransactionScope(TransactionScopeOption.Suppress))
            {
                next();

                tx.Complete();
            }
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("HandlerTransactionScopeWrapperBehavior", typeof(SuppressAmbientTransactionBehavior), "Make sure that any ambient transaction scope is supressed")
            {
                InsertBeforeIfExists("FirstLevelRetriesBehavior");
            }
        }
    }
}