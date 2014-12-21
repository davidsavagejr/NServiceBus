namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Transactions;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class HandlerTransactionScopeWrapperBehavior : IBehavior<IncomingContext>
    {
        readonly TransactionOptions transactionOptions;

        public HandlerTransactionScopeWrapperBehavior(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            if (Transaction.Current != null)
            {
                next();
                return;
            }

            using (var tx = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
            {
                next();

                tx.Complete();
            }
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("HandlerTransactionScopeWrapperBehavior", typeof(HandlerTransactionScopeWrapperBehavior), "Makes sure that the handlers gets wrapped in a transaction scope")
            {
                InsertBefore("FirstLevelRetriesBehavior");

                ContainerRegistration((builder, settings) =>
                {
                    var transactionOptions = new TransactionOptions
                    {
                        IsolationLevel = settings.Get<IsolationLevel>("Transactions.IsolationLevel"),
                        Timeout = settings.Get<TimeSpan>("Transactions.DefaultTimeout")
                    };


                    return new HandlerTransactionScopeWrapperBehavior(transactionOptions);
                });
            }
        }
    }
}