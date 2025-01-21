using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public record OperationPlanStep : PlanStep
{
    public required OperationDefinitionNode Definition { get; init; }

    public required ICompositeNamedType Type { get; init; }

    public required ImmutableHashSet<uint> SelectionSets { get; init; }

    public required string SchemaName { get; init; }
}
