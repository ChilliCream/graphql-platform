using HotChocolate.Language;

#if FUSION
namespace HotChocolate.Fusion.Transport;
#else
namespace HotChocolate.Transport;
#endif

public interface IOperationRequest : IRequestBody
{
    /// <summary>
    /// Gets the ID of a previously persisted operation that should be executed.
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Gets the query string or document containing the operation to execute.
    /// </summary>
    string? Query { get; }

    /// <summary>
    /// Gets the name of the operation to execute.
    /// </summary>
    string? OperationName { get; }

    /// <summary>
    /// Gets the requested error handling mode.
    /// </summary>
    ErrorHandlingMode? OnError { get; }

#if FUSION
    /// <summary>
    /// Gets an <see cref="JsonSegment"/> representing the extension values to include with the
    /// operation.
    /// </summary>
    JsonSegment Extensions { get; }
#else
    /// <summary>
    /// Gets a dictionary containing extension values to include with the operation.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Gets an <see cref="ObjectValueNode"/> representing the extension values to include with the
    /// operation.
    /// </summary>
    ObjectValueNode? ExtensionsNode { get; }
#endif
}
