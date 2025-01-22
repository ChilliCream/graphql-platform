using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning.Nodes3;

public sealed record WorkItem(
    WorkItemKind Kind,
    SelectionSet SelectionSet,
    Lookup? Lookup = null)
{
    public ImmutableHashSet<int> Dependants { get; init; } = ImmutableHashSet<int>.Empty;
}


