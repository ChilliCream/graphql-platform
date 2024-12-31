using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed record UnresolvableSelection(
    ISelectionNode Selection,
    ImmutableStack<SelectionPlanNode> Path);
