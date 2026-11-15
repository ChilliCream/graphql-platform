using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types.Metadata;

/// <summary>
/// Holds which policy requirements are used across the composed schema and the distinct
/// policy names they reference. Stored as a schema feature.
/// </summary>
internal sealed class FusionPolicyUsage(
    FusionPolicyUsageFlags flags,
    ImmutableArray<string> policyNames)
{
    /// <summary>
    /// Gets a value indicating whether any member requires an authenticated user.
    /// </summary>
    public bool UsesAuthenticated => (flags & FusionPolicyUsageFlags.Authenticated) != 0;

    /// <summary>
    /// Gets a value indicating whether any member requires scopes.
    /// </summary>
    public bool UsesScopes => (flags & FusionPolicyUsageFlags.Scopes) != 0;

    /// <summary>
    /// Gets a value indicating whether any member requires policies.
    /// </summary>
    public bool UsesPolicies => (flags & FusionPolicyUsageFlags.Policies) != 0;

    /// <summary>
    /// Gets a value indicating whether any member carries a policy requirement.
    /// </summary>
    public bool IsUsed => flags != FusionPolicyUsageFlags.None;

    /// <summary>
    /// Gets the distinct policy names in ordinal order.
    /// </summary>
    public ImmutableArray<string> PolicyNames { get; } = policyNames;
}
