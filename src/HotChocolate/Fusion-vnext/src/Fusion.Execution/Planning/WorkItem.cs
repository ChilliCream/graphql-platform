using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

internal abstract record WorkItem
{
    public ImmutableHashSet<int> Dependents { get; init; } = [];

    public virtual double Cost => 1;
}
