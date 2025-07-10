using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Manages the lifetime of <see cref="ISourceSchemaClient"/>s
/// that are used within a composite schema request.
/// </summary>
public interface ISourceSchemaClientScope : IAsyncDisposable
{
    /// <summary>
    /// Gets a source schema client for the current <see cref="ISchemaDefinition"/>.
    /// </summary>
    /// <param name="schemaName">
    /// The name of the source schema.
    /// </param>
    /// <param name="operationType">
    /// The operation type for which the client is requested.
    /// </param>
    /// <returns>
    /// An <see cref="ISourceSchemaClient"/> that can be used to
    /// execute requests against the source schema.
    /// </returns>
    ISourceSchemaClient GetClient(string schemaName, OperationType operationType);
}
