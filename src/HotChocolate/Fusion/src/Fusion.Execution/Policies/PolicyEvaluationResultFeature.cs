using System.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// The request feature that carries the <see cref="PolicyEvaluationResult"/> of every variable set.
/// </summary>
internal sealed class PolicyEvaluationResultFeature : RequestFeature
{
    /// <summary>
    /// Gets or sets the results by variable set index.
    /// </summary>
    public ImmutableArray<PolicyEvaluationResult> Results { get; set; }

    protected internal override void Reset()
    {
        Results = default;
    }
}
