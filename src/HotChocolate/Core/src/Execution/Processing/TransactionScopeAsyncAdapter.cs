namespace HotChocolate.Execution.Processing;

internal class TransactionScopeAsyncAdapter : IAsyncTransactionScope
{
    private readonly ITransactionScope _transactionScope;

    public TransactionScopeAsyncAdapter(ITransactionScope transactionScope)
    {
        _transactionScope = transactionScope;
    }

    public ValueTask CompleteAsync()
    {
        _transactionScope.Complete();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _transactionScope.Dispose();
        return ValueTask.CompletedTask;
    }
}
