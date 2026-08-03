using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Describes a single GraphQL request to be sent to a source schema.
/// </summary>
public readonly record struct SourceSchemaClientRequest()
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
    /// Gets the GraphQL operation type (query, mutation, or subscription).
    /// </summary>
    public required OperationType OperationType { get; init; }

    /// <summary>
    /// Gets the GraphQL operation source text to send.
    /// </summary>
    public required OperationSourceText OperationSourceText { get; init; }

    /// <summary>
    /// Gets the parsed syntax tree of <see cref="OperationSourceText"/>.
    /// </summary>
    public required Utf8OperationDocument OperationDocument { get; init; }

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

    /// <summary>
    /// Gets the name of the type that the body of the operation's root selection is selected on,
    /// or <see langword="null"/> when the operation does not select a single root field that the
    /// schema declares.
    /// </summary>
    internal string? LookupTypeName { get; init; }

    /// <summary>
    /// Gets the names of the variables that this operation takes from the client request. Every
    /// other variable of the operation carries a value per variable value set.
    /// </summary>
    internal ImmutableArray<string> ForwardedVariables { get; init; } = [];
}
