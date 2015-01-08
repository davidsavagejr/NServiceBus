namespace NServiceBus
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using NServiceBus.Pipeline.Contexts;

    class MsmqReceiveWithTransactionScopeBehavior : MsmqReceiveBehavior
    {
        public MsmqReceiveWithTransactionScopeBehavior(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        protected override void Invoke(BootstrapContext context, Action<TransportMessage> onMessage)
        {
            var queue = context.Get<MessageQueue>();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
            {
                Message message;

                if (!TryReceiveMessage(() => queue.Receive(TimeSpan.FromSeconds(1), MessageQueueTransactionType.Automatic), context, out message))
                {
                    scope.Complete();
                    return;
                }

                TransportMessage transportMessage;


                if (TryConvertMessage(message, context, out transportMessage))
                {
                    onMessage(transportMessage);
                }

                scope.Complete();
            }
        }

        readonly TransactionOptions transactionOptions;
    }
}