using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning.Nodes3;

public record OperationPlanStep : PlanStep
{
    public required OperationDefinitionNode Definition { get; init; }

    public required ITypeDefinition Type { get; init; }

    public required ImmutableHashSet<uint> SelectionSets { get; init; }

    public required string SchemaName { get; init; }

    public ImmutableHashSet<int> Dependents { get; init; } = [];
}
