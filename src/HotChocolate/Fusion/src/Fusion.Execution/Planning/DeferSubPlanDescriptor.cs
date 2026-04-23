using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Describes a single subplan: the compiled operation for a unique
/// <see cref="DeferUsage"/> set together with the set itself (sorted by
/// <see cref="DeferUsage.Id"/> for stability). Fields whose active defer
/// usage set equals <see cref="DeferUsageSet"/> are fetched by this subplan
/// and delivered to every delivery group in the set.
/// </summary>
internal sealed class DeferSubPlanDescriptor(
    ImmutableArray<DeferUsage> deferUsageSet,
    OperationDefinitionNode operation,
    SelectionPath path,
    DeferSubPlanDescriptor? parent)
{
    /// <summary>
    /// The <see cref="DeferUsage"/> set this subplan is keyed by, sorted
    /// ascending by <see cref="DeferUsage.Id"/>.
    /// </summary>
    public ImmutableArray<DeferUsage> DeferUsageSet { get; } = deferUsageSet;

    /// <summary>
    /// The compiled operation for this subplan.
    /// </summary>
    public OperationDefinitionNode Operation { get; internal set; } = operation;

    /// <summary>
    /// The path where the subplan's data is inserted in the response tree.
    /// Derived from the deepest <see cref="DeferUsage.Path"/> in the set.
    /// </summary>
    public SelectionPath Path { get; } = path;

    /// <summary>
    /// The parent subplan for nested <c>@defer</c>, or <c>null</c> for a
    /// top-level subplan. Determined by walking each set member's
    /// <see cref="DeferUsage.Parent"/> chain and finding the first already-
    /// emitted subplan whose set contains a matching ancestor.
    /// </summary>
    public DeferSubPlanDescriptor? Parent { get; } = parent;

    /// <summary>
    /// Plan-scope requirements this subplan sources from the parent plan's
    /// variable flow. Populated by the parent plan's requirement system
    /// during parent planning so that the parent plan produces every value
    /// the subplan needs before the subplan executes. A resolved entry maps
    /// a variable name used inside the subplan to a selection in the parent
    /// plan's result tree. Keyed by requirement name to dedup across groups
    /// that share the same requirement.
    /// </summary>
    public SortedDictionary<string, OperationRequirement> Requirements { get; } =
        new(StringComparer.Ordinal);
}
