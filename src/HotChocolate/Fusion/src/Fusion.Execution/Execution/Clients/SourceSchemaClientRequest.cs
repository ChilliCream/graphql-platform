using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Describes a single GraphQL request to be sent to a source schema.
/// </summary>
public sealed class SourceSchemaClientRequest
{
    /// <summary>
    /// Gets the execution node that produced this request.
    /// </summary>
    public required ExecutionNode Node { get; init; }

    /// <summary>
    /// Gets the name of the source schema this request targets.
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// Gets the optional batching group identifier assigned at planning time.
    /// When set, the <see cref="ISourceSchemaScheduler"/> holds this request until
    /// all nodes in the same group have submitted or been skipped, then dispatches
    /// them together via <see cref="ISourceSchemaClient.ExecuteBatchAsync"/>.
    /// </summary>
    public int? BatchingGroupId { get; init; }

    /// <summary>
    /// Gets the GraphQL operation type (query, mutation, or subscription).
    /// </summary>
    public required OperationType OperationType { get; init; }

    /// <summary>
    /// Gets the GraphQL operation source text to send.
    /// </summary>
    public required string OperationSourceText { get; init; }

    /// <summary>
    /// Gets the variable value sets for this operation. Multiple entries indicate
    /// that the operation should be executed once per variable set (variable batching).
    /// </summary>
    public ImmutableArray<VariableValues> Variables { get; init; } = [];

    /// <summary>
    /// Gets whether the operation contains variables that include the Upload scalar,
    /// requiring multipart form encoding.
    /// </summary>
    public bool RequiresFileUpload { get; init; }
}
