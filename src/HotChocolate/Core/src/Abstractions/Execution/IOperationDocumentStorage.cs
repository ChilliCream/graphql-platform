using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a storage for operation documents.
/// </summary>
public interface IOperationDocumentStorage
{
    /// <summary>
    /// Tries to read an operation document from the storage.
    /// If the document does not exist <c>null</c> is returned.
    /// </summary>
    /// <param name="documentId">
    /// The id of the document to read.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the operation document or <c>null</c> if the document does not exist.
    /// </returns>
    ValueTask<IOperationDocument?> TryReadAsync(
        OperationDocumentId documentId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves an operation document to the storage.
    /// </summary>
    /// <param name="documentId">
    /// The id of the document to save.
    /// </param>
    /// <param name="document">
    /// The document to save.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask SaveAsync(
        OperationDocumentId documentId,
        IOperationDocument document,
        CancellationToken cancellationToken = default);
}