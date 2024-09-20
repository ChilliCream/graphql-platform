using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Transport.Serialization;

namespace HotChocolate.Transport;

/// <summary>
/// Represents a GraphQL operation request that can be sent over a WebSocket connection.
/// </summary>
public readonly struct VariableBatchRequest : IOperationRequest, IEquatable<OperationRequest>
{
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
        IReadOnlyList<ObjectValueNode>? variables,
        ObjectValueNode? extensions)
    {
        Query = query;
        Id = id;
        OperationName = operationName;
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
        IReadOnlyList<IReadOnlyDictionary<string, object?>>? variables = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        Query = query;
        Id = id;
        OperationName = operationName;
        Variables = variables;
        Extensions = extensions;
    }

    /// <summary>
    /// Empty Operation Request.
    /// </summary>
    public static OperationRequest Empty { get; } = new();

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

    /// <summary>
    /// Writes a serialized version of this request to a <see cref="Utf8JsonWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    public void WriteTo(Utf8JsonWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        Utf8JsonWriterHelper.WriteOperationRequest(writer, this);
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
    public bool Equals(OperationRequest other)
        => Id == other.Id &&
            Query == other.Query &&
            Equals(Variables, other.Variables) &&
            Equals(Extensions, other.Extensions) &&
            Equals(VariablesNode, other.VariablesNode) &&
            Equals(ExtensionsNode, other.ExtensionsNode);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is OperationRequest other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(Id, Query, Variables, Extensions, VariablesNode, ExtensionsNode);

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
