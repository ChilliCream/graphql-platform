using System.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// The request feature that carries the <see cref="PolicyDecisions"/> of every variable set.
/// </summary>
internal sealed class PolicyDecisionsInfo : RequestFeature
{
    /// <summary>
    /// Gets or sets the decisions by variable set index. An entry is <c>null</c> if every
    /// selection of the variable set is allowed.
    /// </summary>
    public ImmutableArray<PolicyDecisions?> Decisions { get; set; }

    protected internal override void Reset()
    {
        Decisions = default;
    }
}
