using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed record FieldRequirementsPlanNode(
    FieldRequirements Requirements,
    ImmutableStack<SelectionPlanNode> Path);
