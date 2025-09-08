using HotChocolate.Language;

namespace HotChocolate.PersistedOperations;

/// <summary>
/// Provides the hash of an operation document.
/// </summary>
public interface IOperationDocumentHashProvider
{
    /// <summary>
    /// Gets the hash of the operation document.
    /// </summary>
    OperationDocumentHash Hash { get; }
}
