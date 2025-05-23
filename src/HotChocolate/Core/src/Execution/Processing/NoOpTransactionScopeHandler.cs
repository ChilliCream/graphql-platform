
namespace HotChocolate.Execution.Processing;

/// <summary>
/// This transaction scope handler represents creates
/// a non transactional mutation transaction scope.
/// </summary>
internal sealed class NoOpTransactionScopeHandler : IAsyncTransactionScopeHandler
{
    private readonly NoOpTransactionScope _noOpTransaction = new();

    Task<IAsyncTransactionScope> IAsyncTransactionScopeHandler.CreateAsync(IRequestContext context)
        => Task.FromResult<IAsyncTransactionScope>(_noOpTransaction);
}
