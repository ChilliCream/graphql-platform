using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public abstract record WorkItem
{
    public ImmutableHashSet<int> Dependents { get; init; } = [];
}
