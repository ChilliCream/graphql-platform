namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a mutation transaction scope.
/// </summary>
public interface IAsyncTransactionScope : IAsyncDisposable
{
    /// <summary>
    /// Completes a transaction (commits or discards the changes).
    /// </summary>
    ValueTask CompleteAsync();
}
