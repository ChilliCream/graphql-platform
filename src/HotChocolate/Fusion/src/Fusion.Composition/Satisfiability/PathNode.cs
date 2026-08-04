using HotChocolate.Fusion.Collections;

namespace HotChocolate.Fusion.Satisfiability;

internal sealed class PathNode(SatisfiabilityPathItem item, PathNode? parent)
{
    public SatisfiabilityPathItem Item { get; } = item;

    public PathNode? Parent { get; } = parent;
}
