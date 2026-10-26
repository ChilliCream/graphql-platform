using System.Collections.Immutable;
using HotChocolate.Fusion.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// The position at which a path item holds a visited object. A walk along a path that ends at
/// <see cref="Item"/> stops there and asks the anchor which schemas can hold the object.
/// </summary>
internal abstract class PathAnchor
{
    /// <summary>
    /// Gets the path item that holds the object.
    /// </summary>
    public abstract SatisfiabilityPathItem Item { get; }

    /// <summary>
    /// Gets whether a viewer field below the object is re-rooted at <c>Query.viewer</c>.
    /// </summary>
    public abstract bool ReroutesViewerFields { get; }

    /// <summary>
    /// Determines whether <paramref name="schemaName"/> can hold the object at its position without
    /// data from <paramref name="unavailableSchemaNames"/>.
    /// </summary>
    public abstract bool CanHold(string schemaName, ImmutableHashSet<string> unavailableSchemaNames);

    /// <summary>
    /// Gets the selection set that <paramref name="schemaName"/> provides for the object when it holds
    /// it without data from <paramref name="unavailableSchemaNames"/>, or <c>null</c> if it provides none.
    /// </summary>
    public abstract SelectionSetNode? GetProvidedSelectionSet(
        string schemaName,
        ImmutableHashSet<string> unavailableSchemaNames);

    /// <summary>
    /// Determines whether the call that holds the object in the schema of <see cref="Item"/> was made
    /// without data from <paramref name="unavailableSchemaNames"/>.
    /// </summary>
    public abstract bool IsHolderAvailable(ImmutableHashSet<string> unavailableSchemaNames);

    /// <summary>
    /// Determines whether <paramref name="schemaName"/> holds the object without data from
    /// <paramref name="unavailableSchemaNames"/> only through calls that depend on a call re-rooted at
    /// <c>Query.viewer</c>.
    /// </summary>
    public abstract bool IsViewerOnly(string schemaName, ImmutableHashSet<string> unavailableSchemaNames);

    /// <summary>
    /// Gets an anchor that answers like this one for calls that do not depend on a call re-rooted at
    /// <c>Query.viewer</c>.
    /// </summary>
    public PathAnchor WithoutReRootedCalls() => new NotReRootedAnchor(this);

    private sealed class NotReRootedAnchor(PathAnchor anchor) : PathAnchor
    {
        public override SatisfiabilityPathItem Item => anchor.Item;

        public override bool ReroutesViewerFields => false;

        public override bool CanHold(string schemaName, ImmutableHashSet<string> unavailableSchemaNames)
            => anchor.CanHold(schemaName, unavailableSchemaNames)
                && !anchor.IsViewerOnly(schemaName, unavailableSchemaNames);

        public override SelectionSetNode? GetProvidedSelectionSet(
            string schemaName,
            ImmutableHashSet<string> unavailableSchemaNames)
            => anchor.GetProvidedSelectionSet(schemaName, unavailableSchemaNames);

        public override bool IsHolderAvailable(ImmutableHashSet<string> unavailableSchemaNames)
            => anchor.IsHolderAvailable(unavailableSchemaNames)
                && !anchor.IsViewerOnly(anchor.Item.SchemaName, unavailableSchemaNames);

        public override bool IsViewerOnly(string schemaName, ImmutableHashSet<string> unavailableSchemaNames)
            => anchor.IsViewerOnly(schemaName, unavailableSchemaNames);
    }
}
