using System.IO.Hashing;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Describes one authorization policy referenced by an operation plan.
/// </summary>
public sealed record PolicyPlanEntry
{
    /// <summary>
    /// Gets the name of the referenced policy.
    /// </summary>
    public required string PolicyName { get; init; }

    /// <summary>
    /// Gets the nonzero content fingerprint of the nullable resource requirement used to build the plan.
    /// </summary>
    public required ulong RequirementHash { get; init; }

    internal static ulong ComputeRequirementHash(SelectionSetNode? selectionSet)
    {
        var hash = selectionSet is null
            ? XxHash64.HashToUInt64([])
            : XxHash64.HashToUInt64(
                Encoding.UTF8.GetBytes(selectionSet.ToString(indented: false)));

        return hash == 0 ? 1 : hash;
    }
}
