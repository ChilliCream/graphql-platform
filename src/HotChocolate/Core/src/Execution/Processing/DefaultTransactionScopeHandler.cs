using System.Transactions;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline
{
    /// <summary>
    /// Represents the default mutation transaction scope handler implementation.
    /// </summary>
    public class DefaultTransactionScopeHandler : ITransactionScopeHandler
    {
        private readonly NoOpTransactionScope _noOpTransaction = new();

        /// <summary>
        /// Creates a new transaction scope for the current
        /// request represented by the <see cref="IRequestContext"/>.
        /// </summary>
        /// <param name="context">
        /// The GraphQL request context.
        /// </param>
        /// <returns>
        /// Returns a new <see cref="ITransactionScope"/>.
        /// </returns>
        public virtual ITransactionScope Create(IRequestContext context)
        {
            if (context.Operation.Type == OperationType.Mutation)
            {
                return new DefaultTransactionScope(
                    context,
                    new TransactionScope(
                        TransactionScopeOption.Required,
                        new TransactionOptions
                        {
                            IsolationLevel = IsolationLevel.ReadCommitted
                        }));
            }

            return _noOpTransaction;
        }
    }
}
