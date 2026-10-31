using System.Collections.Immutable;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// The policy requirements of the protected selections of an operation plan and the synthetic
/// variables that skip denied selections in source schema requests.
/// </summary>
public sealed class PolicyPlan
{
    internal PolicyPlan(
        ImmutableArray<PolicyDescriptor> descriptors,
        ImmutableArray<PolicyVariable> variables)
    {
        Descriptors = descriptors;
        Variables = variables;
        HasUnresolvedPolicies = ContainsUnresolvedPolicy(descriptors);
    }

    /// <summary>
    /// Gets one descriptor for every directive occurrence of every protected selection.
    /// </summary>
    public ImmutableArray<PolicyDescriptor> Descriptors { get; }

    /// <summary>
    /// Gets the synthetic variables in the order they were allocated.
    /// </summary>
    public ImmutableArray<PolicyVariable> Variables { get; }

    /// <summary>
    /// Gets a value that indicates whether any descriptor refers to a policy name that no
    /// provider resolved.
    /// </summary>
    internal bool HasUnresolvedPolicies { get; }

    private static bool ContainsUnresolvedPolicy(ImmutableArray<PolicyDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            if (descriptor.Policy is UnresolvedPolicy)
            {
                return true;
            }
        }

        return false;
    }
}
