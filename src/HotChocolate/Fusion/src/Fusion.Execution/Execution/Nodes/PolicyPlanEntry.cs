using System.IO.Hashing;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Describes one authorization policy referenced by an operation plan.
/// </summary>
/// <remarks>
/// The operation plan carries one entry per distinct policy name and requirement hash pair, so that
/// a request can resolve every policy instance it needs ahead of execution and a changed requirement
/// can be recognized against every requirement the plan was built for.
/// </remarks>
public sealed record PolicyPlanEntry
{
    /// <summary>
    /// Gets the name of the referenced policy.
    /// </summary>
    public required string PolicyName { get; init; }

    /// <summary>
    /// Gets a content hash of the resource requirement this plan was built against for this
    /// policy, or <c>0</c> when the plan was built assuming the policy has no resource
    /// requirement.
    /// </summary>
    public required ulong RequirementHash { get; init; }

    internal static ulong ComputeRequirementHash(SelectionSetNode selectionSet)
        => XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(selectionSet.ToString(indented: false)));
}
