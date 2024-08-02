namespace HotChocolate.Execution.Internal;

/// <summary>
/// The is an internal request executor resolver that is not meant for public usage.
/// </summary>
public interface IInternalRequestExecutorResolver
{
    /// <summary>
    /// Gets or creates the request executor that is associated with the
    /// given configuration <paramref name="schemaName" />.
    /// </summary>
    /// <param name="schemaName">
    /// The schema name.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a request executor that is associated with the
    /// given configuration <paramref name="schemaName" />.
    /// </returns>
    ValueTask<IRequestExecutor> GetRequestExecutorNoLockAsync(
        string? schemaName = default,
        CancellationToken cancellationToken = default);
}
