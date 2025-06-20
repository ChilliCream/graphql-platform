using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

public abstract record WorkItem
{
    public ImmutableHashSet<int> Dependents { get; init; } = [];
}
