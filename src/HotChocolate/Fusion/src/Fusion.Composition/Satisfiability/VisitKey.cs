using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Identifies everything a visit of an object type reads from its access path, so that work items
/// with equal keys log the same field errors and enqueue work items with equal keys.
/// </summary>
/// <param name="ObjectType">The visited object type.</param>
/// <param name="SchemaName">The schema the object is held in, or <c>null</c> at a root type.</param>
/// <param name="Reach">
/// The schemas, among those the visit and its children read, that the object can be entered in from
/// its position through a lookup, a parent entity call, the viewer fallback or the one-to-one path.
/// </param>
/// <param name="ReachProvides">
/// The selection sets that the schemas of <paramref name="Reach"/> provide for the object when they
/// re-traverse its path, or <c>null</c> if none provides one.
/// </param>
/// <param name="Leaf">
/// The path item holding the object when the visit reads more of it than its schema; otherwise <c>null</c>.
/// </param>
/// <param name="ViewerFallback">
/// Whether a viewer field below the object is re-rooted at <c>Query.viewer</c>, which holds on mutation
/// and subscription paths of objects that reach a viewer field missing in a schema of <c>Query.viewer</c>;
/// otherwise <c>false</c>.
/// </param>
internal sealed record VisitKey(
    MutableObjectTypeDefinition ObjectType,
    string? SchemaName,
    SchemaSet Reach,
    SchemaSelectionSets? ReachProvides,
    VisitLeaf? Leaf,
    bool ViewerFallback);
