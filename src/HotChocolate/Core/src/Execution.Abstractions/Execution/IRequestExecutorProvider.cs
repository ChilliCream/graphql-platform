using System.Collections.Immutable;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a provider for GraphQL request executors.
/// </summary>
public interface IRequestExecutorProvider
{
    /// <summary>
    /// Gets the names of all registered schemas.
    /// </summary>
    ImmutableArray<string> SchemaNames { get; }

    /// <summary>
    /// Gets a GraphQL request executor for the given schema name.
    /// </summary>
    /// <param name="schemaName">
    /// The name of the schema to get an executor for.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a GraphQL request executor.
    /// </returns>
    public ValueTask<IRequestExecutor> GetExecutorAsync(
        string? schemaName = null,
        CancellationToken cancellationToken = default);
}
