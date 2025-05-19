using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public record OperationExecutionNode : ExecutionNode
{
    public required OperationDefinitionNode Definition { get; init; }

    public required string SchemaName { get; init; }

    public ImmutableArray<ExecutionNode> Dependencies { get; init; } = [];

    public ImmutableArray<ExecutionNode> Dependents { get; init; } = [];

    public ImmutableArray<OperationRequirement> Requirements { get; init; } = [];
}
