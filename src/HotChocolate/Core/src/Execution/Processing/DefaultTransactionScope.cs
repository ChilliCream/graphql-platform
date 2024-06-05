using System.Transactions;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents the default mutation transaction scope implementation.
/// </summary>
public class DefaultTransactionScope : ITransactionScope
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultTransactionScope"/>.
    /// </summary>
    /// <param name="context">
    /// The GraphQL request context.
    /// </param>
    /// <param name="transaction">
    /// The mutation transaction scope.
    /// </param>
    public DefaultTransactionScope(IRequestContext context, TransactionScope transaction)
    {
        Context = context;
        Transaction = transaction;
    }

    /// <summary>
    /// Gets GraphQL request context.
    /// </summary>
    protected IRequestContext Context { get; }

    /// <summary>
    /// Gets the mutation transaction scope.
    /// </summary>
    protected TransactionScope Transaction { get; }

    /// <summary>
    /// Completes a transaction (commits or discards the changes).
    /// </summary>
    public void Complete()
    {
        if (Context.Result is OperationResult { Data: not null, Errors: null or { Count: 0, }, })
        {
            Transaction.Complete();
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing,
    /// releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Transaction.Dispose();
    }
}
