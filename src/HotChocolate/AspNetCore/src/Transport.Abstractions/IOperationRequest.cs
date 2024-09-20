using HotChocolate.Language;

namespace HotChocolate.Transport;

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
    /// Gets a dictionary containing extension values to include with the operation.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Gets an <see cref="ObjectValueNode"/> representing the extension values to include with the
    /// operation.
    /// </summary>
    ObjectValueNode? ExtensionsNode { get; }
}
