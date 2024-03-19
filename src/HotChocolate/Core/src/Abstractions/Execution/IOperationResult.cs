using System.Collections.Generic;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a query result object.
/// </summary>
public interface IOperationResult : IExecutionResult
{
    /// <summary>
    /// Gets the index of the request that corresponds to this result.
    /// </summary>
    int? RequestIndex { get; }
    
    /// <summary>
    /// Gets the index of of the variable set that corresponds to this result.
    /// </summary>
    int? VariableIndex { get; }
    
    /// <summary>
    /// A string that was passed to the label argument of the @defer or @stream
    /// directive that corresponds to this results.
    /// </summary>
    /// <value></value>
    string? Label { get; }

    /// <summary>
    ///  A path to the insertion point that informs the client how to patch a
    /// subsequent delta payload into the original payload.
    /// </summary>
    /// <value></value>
    Path? Path { get; }

    /// <summary>
    /// The data that is being delivered.
    /// </summary>
    /// <value></value>
    IReadOnlyDictionary<string, object?>? Data { get; }

    /// <summary>
    /// The `items` entry in a stream payload is a list of results from the execution of
    /// the associated @stream directive. This output will be a list of the same type of
    /// the field with the associated `@stream` directive. If `items` is set to `null`,
    /// it indicates that an error has caused a `null` to bubble up to a field higher
    /// than the list field with the associated `@stream` directive.
    /// </summary>
    IReadOnlyList<object?>? Items { get; }

    /// <summary>
    /// Gets the GraphQL errors of the result.
    /// </summary>
    IReadOnlyList<IError>? Errors { get; }

    /// <summary>
    /// Gets the additional information that are passed along
    /// with the result and will be serialized for transport.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Gets the incremental patches provided with this result.
    /// </summary>
    IReadOnlyList<IOperationResult>? Incremental { get; }

    /// <summary>
    /// A boolean that is present and <c>true</c> when there are more payloads
    /// that will be sent for this operation. The last payload in a multi payload response
    /// should return HasNext: <c>false</c>.
    /// HasNext is null for single-payload responses to preserve backwards compatibility.
    /// </summary>
    /// <value></value>
    bool? HasNext { get; }
    
    /// <summary>
    /// Specifies if data was explicitly set.
    /// If <c>false</c> the data was not set (including null).
    /// </summary>
    bool IsDataSet { get; }

    /// <summary>
    /// Serializes this GraphQL result into a dictionary.
    /// </summary>
    IReadOnlyDictionary<string, object?> ToDictionary();
}
