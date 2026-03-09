using System.Text.Json;

namespace HotChocolate.Language;

/// <summary>
/// Represents the parsed GraphQL request JSON object.
/// </summary>
public sealed record GraphQLRequest : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLRequest"/>.
    /// </summary>
    /// <param name="document">
    /// The GraphQL operation document that contains the operation definitions.
    /// </param>
    /// <param name="documentId">
    /// The GraphQL operation document id which references
    /// an operation document in a persisted operation document store.
    /// </param>
    /// <param name="documentHash">
    /// The hash of the GraphQL operation document.
    /// </param>
    /// <param name="operationName">
    /// The name of an operation in the operation document that shall be executed.
    /// </param>
    /// <param name="errorHandlingMode">
    /// The requested error handling mode.
    /// </param>
    /// <param name="variables">
    /// A list of variables for the operation.
    /// </param>
    /// <param name="extensions">
    /// The GraphQL request extensions map.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document"/> is <c>null</c> and <paramref name="documentId"/> is <c>null</c>.
    /// </exception>
    public GraphQLRequest(
        DocumentNode? document,
        OperationDocumentId? documentId = null,
        OperationDocumentHash? documentHash = null,
        string? operationName = null,
        ErrorHandlingMode? errorHandlingMode = null,
        JsonDocument? variables = null,
        JsonDocument? extensions = null)
    {
        if (document is null && documentId?.IsEmpty is not false)
        {
            throw new ArgumentNullException(nameof(document));
        }

        DocumentId = documentId;
        DocumentHash = documentHash;
        Document = document;
        OperationName = operationName;
        ErrorHandlingMode = errorHandlingMode;
        Variables = variables;
        Extensions = extensions;
    }

    /// <summary>
    /// Gets the GraphQL operation document id which references
    /// an operation document in a persisted operation document store.
    /// </summary>
    public OperationDocumentId? DocumentId { get; init; }

    /// <summary>
    /// Gets the hash of the GraphQL operation document.
    /// </summary>
    public OperationDocumentHash? DocumentHash { get; init; }

    /// <summary>
    /// Gets the GraphQL operation document that contains the operation definitions.
    /// </summary>
    public DocumentNode? Document { get; init; }

    /// <summary>
    /// Gets the name of an operation in the operation document that shall be executed.
    /// If this property is <c>null</c> the document only contains a single operation.
    /// </summary>
    public string? OperationName { get; init; }

    /// <summary>
    /// Get the requested error handling mode.
    /// </summary>
    public ErrorHandlingMode? ErrorHandlingMode { get; init; }

    /// <summary>
    /// Gets a list of variables for the operation.
    /// For a standard GraphQL request this list will contain a single variable set or be <c>null</c>.
    /// </summary>
    public JsonDocument? Variables { get; init; }

    /// <summary>
    /// Gets the GraphQL request extensions map.
    /// </summary>
    public JsonDocument? Extensions { get; init; }

    /// <summary>
    /// Gets the memory owner for the variables JSON document buffer.
    /// This will be disposed when the request is disposed.
    /// </summary>
    public IDisposable? VariablesMemoryOwner { get; init; }

    /// <summary>
    /// Gets the memory owner for the extensions JSON document buffer.
    /// This will be disposed when the request is disposed.
    /// </summary>
    public IDisposable? ExtensionsMemoryOwner { get; init; }

    /// <summary>
    /// Disposes the request and releases all associated resources including
    /// the variables and extensions JSON documents and their memory owners.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Variables?.Dispose();
            Extensions?.Dispose();
            VariablesMemoryOwner?.Dispose();
            ExtensionsMemoryOwner?.Dispose();
            _disposed = true;
        }
    }
}
