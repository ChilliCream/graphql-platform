using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed record WorkItem(
    WorkItemKind Kind,
    SelectionSet SelectionSet,
    Lookup? Lookup = null)
{
    public ImmutableHashSet<int> Dependents { get; init; } = [];
}
