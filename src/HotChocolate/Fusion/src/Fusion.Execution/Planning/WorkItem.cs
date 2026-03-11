using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

internal abstract record WorkItem
{
    public ImmutableHashSet<int> Dependents { get; init; } = [];

    /// <summary>
    /// Depth of the operation step that produced this work item.
    /// </summary>
    public int ParentDepth { get; init; }

    /// <summary>
    /// Estimated depth of the operation step this work item will create.
    /// </summary>
    public virtual int EstimatedDepth => ParentDepth + 1;

    public virtual double Cost => 1;
}
