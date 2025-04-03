namespace HotChocolate.Execution.Processing;

public class TransactionScopeHandlerAsyncAdapter : IAsyncTransactionScopeHandler
{
    private readonly ITransactionScopeHandler _transactionScopeHandler;

    public TransactionScopeHandlerAsyncAdapter(ITransactionScopeHandler transactionScopeHandler)
    {
        _transactionScopeHandler = transactionScopeHandler;
    }

    public Task<IAsyncTransactionScope> CreateAsync(IRequestContext context)
    {
        IAsyncTransactionScope asyncTransactionScope = new TransactionScopeAsyncAdapter(_transactionScopeHandler.Create(context));
        return Task.FromResult(asyncTransactionScope);
    }
}
