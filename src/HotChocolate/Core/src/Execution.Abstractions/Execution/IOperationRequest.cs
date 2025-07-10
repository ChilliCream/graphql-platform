using HotChocolate.Language;

namespace HotChocolate.Execution;

/// <summary>
/// Defines the basic properties for a GraphQL operation request.
/// </summary>
public interface IOperationRequest : IExecutionRequest
{
    /// <summary>
    /// Gets the GraphQL request document.
    /// </summary>
    IOperationDocument? Document { get; }

    /// <summary>
    /// Gets the GraphQL request document ID.
    /// </summary>
    OperationDocumentId DocumentId { get; }

    /// <summary>
    /// Gets GraphQL request document hash.
    /// </summary>
    OperationDocumentHash DocumentHash { get; }

    /// <summary>
    /// A name of an operation in the GraphQL request document that shall be executed;
    /// or, <c>null</c> if the document only contains a single operation.
    /// </summary>
    string? OperationName { get; }

    /// <summary>
    /// Gets the GraphQL request extension data.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// GraphQL request flags allow limiting the GraphQL executor capabilities.
    /// </summary>
    RequestFlags Flags { get; }
}
