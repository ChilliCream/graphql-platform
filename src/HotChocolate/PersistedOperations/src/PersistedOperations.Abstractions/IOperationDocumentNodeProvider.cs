using HotChocolate.Language;

namespace HotChocolate.PersistedOperations;

/// <summary>
/// Provides the document syntax node of an operation document.
/// </summary>
public interface IOperationDocumentNodeProvider
{
    /// <summary>
    /// Gets the document syntax node of the operation document.
    /// </summary>
    DocumentNode Document { get; }
}
