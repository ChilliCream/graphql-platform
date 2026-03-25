#if FUSION
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Fusion.Transport.Serialization;

namespace HotChocolate.Fusion.Transport;
#else
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Transport.Serialization;

namespace HotChocolate.Transport;
#endif

/// <summary>
/// Represents a GraphQL operation request that can be sent over a WebSocket connection.
/// </summary>
public sealed class VariableBatchRequest : IOperationRequest, IEquatable<VariableBatchRequest>
{
#if FUSION
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationRequest"/> struct.
    /// </summary>
    /// <param name="query">
    /// The query document containing the operation to execute.
    /// </param>
    /// <param name="id">
    /// The ID of a previously persisted operation that should be executed.
    /// </param>
    /// <param name="operationName">
    /// The name of the operation to execute.
    /// </param>
    /// <param name="onError">
    /// The requested error handling mode.
    /// </param>
    /// <param name="variables">
    /// A list of dictionaries representing the sets of variable values to use when executing the operation.
    /// </param>
    /// <param name="extensions">
    /// A dictionary containing extension values to include with the operation.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if the query, ID, and extensions parameters are all null.
    /// </exception>
    public VariableBatchRequest(
        string? query,
        string? id,
        string? operationName,
        ErrorHandlingMode? onError,
        JsonSegment variables,
        JsonSegment extensions)
    {
        Query = query;
        Id = id;
        OperationName = operationName;
        OnError = onError;
        Variables = variables;
        Extensions = extensions;
    }
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationRequest"/> struct.
    /// </summary>
    /// <param name="query">
    /// The query document containing the operation to execute.
    /// </param>
    /// <param name="id">
    /// The ID of a previously persisted operation that should be executed.
    /// </param>
    /// <param name="operationName">
    /// The name of the operation to execute.
    /// </param>
    /// <param name="onError">
    /// The requested error handling mode.
    /// </param>
    /// <param name="variables">
    /// A list of dictionaries representing the sets of variable values to use when executing the operation.
    /// </param>
    /// <param name="extensions">
    /// A dictionary containing extension values to include with the operation.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if the query, ID, and extensions parameters are all null.
    /// </exception>
    public VariableBatchRequest(
        string? query,
        string? id,
        string? operationName,
        ErrorHandlingMode? onError,
        IReadOnlyList<ObjectValueNode>? variables,
        ObjectValueNode? extensions)
    {
        Query = query;
        Id = id;
        OperationName = operationName;
        OnError = onError;
        VariablesNode = variables;
        ExtensionsNode = extensions;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationRequest"/> struct.
    /// </summary>
    /// <param name="query">
    /// The query string containing the operation to execute.
    /// </param>
    /// <param name="id">
    /// The ID of a previously persisted operation that should be executed.
    /// </param>
    /// <param name="operationName">
    /// The name of the operation to execute.
    /// </param>
    /// <param name="onError">
    /// The requested error handling mode.
    /// </param>
    /// <param name="variables">
    /// A list of dictionaries representing the sets of variable values to use when executing the operation.
    /// </param>
    /// <param name="extensions">
    /// A dictionary containing extension values to include with the operation.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if the query, ID, and extensions parameters are all null.
    /// </exception>
    public VariableBatchRequest(
        string? query = null,
        string? id = null,
        string? operationName = null,
        ErrorHandlingMode? onError = null,
        IReadOnlyList<IReadOnlyDictionary<string, object?>>? variables = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        Query = query;
        Id = id;
        OperationName = operationName;
        OnError = onError;
        Variables = variables;
        Extensions = extensions;
    }
#endif

    /// <summary>
    /// Gets the ID of a previously persisted operation that should be executed.
    /// </summary>
    public string? Id { get; }

    /// <summary>
    /// Gets the query string or document containing the operation to execute.
    /// </summary>
    public string? Query { get; }

    /// <summary>
    /// Gets the name of the operation to execute.
    /// </summary>
    public string? OperationName { get; }

    /// <summary>
    /// Gets the requested error handling mode.
    /// </summary>
    public ErrorHandlingMode? OnError { get; }

#if FUSION
    /// <summary>
    /// Gets a list of dictionaries representing the sets of variable values to use when executing the operation.
    /// </summary>
    public JsonSegment Variables { get; }

    /// <summary>
    /// Gets a dictionary containing extension values to include with the operation.
    /// </summary>
    public JsonSegment Extensions { get; }
#else
    /// <summary>
    /// Gets a list of dictionaries representing the sets of variable values to use when executing the operation.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>>? Variables { get; }

    /// <summary>
    /// Gets a list of <see cref="ObjectValueNode"/> representing the sets of variable values to
    /// use when executing the operation.
    /// </summary>
    public IReadOnlyList<ObjectValueNode>? VariablesNode { get; }

    /// <summary>
    /// Gets a dictionary containing extension values to include with the operation.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Gets an <see cref="ObjectValueNode"/> representing the extension values to include with the
    /// operation.
    /// </summary>
    public ObjectValueNode? ExtensionsNode { get; }
#endif

    /// <summary>
    /// Writes a serialized version of this request to a JSON writer.
    /// </summary>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
#if FUSION
    public void WriteTo(JsonWriter writer)
#else
    public void WriteTo(Utf8JsonWriter writer)
#endif
    {
        ArgumentNullException.ThrowIfNull(writer);

        Utf8JsonWriterHelper.WriteVariableBatchRequest(writer, this);
    }

    /// <summary>
    /// Determines whether this <see cref="OperationRequest"/> object is equal to another object.
    /// </summary>
    /// <param name="other">
    /// The object to compare with this <see cref="OperationRequest"/> object.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the two objects are equal; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(VariableBatchRequest? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id
            && Query == other.Query
            && Variables.Equals(other.Variables)
            && Extensions.Equals(other.Extensions);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is OperationRequest other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(Id, Query, Variables, Extensions);

    /// <summary>
    /// Determines whether two <see cref="OperationRequest"/> objects are equal.
    /// </summary>
    /// <param name="left">
    /// The first <see cref="OperationRequest"/> object to compare.
    /// </param>
    /// <param name="right">
    /// The second <see cref="OperationRequest"/> object to compare.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the two objects are equal; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(VariableBatchRequest left, VariableBatchRequest right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="OperationRequest"/> objects are not equal.
    /// </summary>
    /// <param name="left">
    /// The first <see cref="OperationRequest"/> object to compare.
    /// </param>
    /// <param name="right">
    /// The second <see cref="OperationRequest"/> object to compare.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the two objects are not equal; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(VariableBatchRequest left, VariableBatchRequest right)
        => !left.Equals(right);
}
