namespace HotChocolate.PersistedOperations.FileSystem;

/// <summary>
/// Responsible for mapping an operation document identifier to a file path.
/// </summary>
public interface IOperationDocumentFileMap
{
    /// <summary>
    /// Gets the root directory on which the map operates.
    /// </summary>
    string Root { get; }

    /// <summary>
    /// Maps an operation document identifier to the file path
    /// containing the operation document.
    /// </summary>
    /// <param name="documentId">The operation document identifier.</param>
    /// <returns>The file path of the operation document.</returns>
    string MapToFilePath(string documentId);
}
