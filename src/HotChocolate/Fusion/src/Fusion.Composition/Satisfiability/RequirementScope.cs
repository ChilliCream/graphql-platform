using HotChocolate.Fusion.Collections;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// The part of the access path that a requirement walk reads above its first item, and whether the
/// walk may take data from calls re-rooted at <c>Query.viewer</c>.
/// </summary>
/// <param name="Anchor">
/// The anchor that the walk stops at, or <c>null</c> to walk the path up to the root.
/// </param>
/// <param name="ArrivalPath">
/// The access path items, from the leaf to the root, that a walk without an <paramref name="Anchor"/>
/// reads, or <c>null</c> if it treats every call on the path as available.
/// </param>
/// <param name="AllowsReRootedCalls">
/// Whether the walk may take data from calls re-rooted at <c>Query.viewer</c>.
/// </param>
internal sealed record RequirementScope(
    PathAnchor? Anchor,
    SatisfiabilityPathItem[]? ArrivalPath,
    bool AllowsReRootedCalls)
{
    /// <summary>
    /// Gets the scope of a walk without an anchor that treats every call on the path as available.
    /// </summary>
    public static RequirementScope Unanchored { get; } = new(null, null, true);

    /// <summary>
    /// Gets this scope for a walk that takes no data from calls re-rooted at <c>Query.viewer</c>.
    /// </summary>
    public RequirementScope WithoutReRootedCalls()
        => this with { Anchor = Anchor?.WithoutReRootedCalls(), AllowsReRootedCalls = false };
}
