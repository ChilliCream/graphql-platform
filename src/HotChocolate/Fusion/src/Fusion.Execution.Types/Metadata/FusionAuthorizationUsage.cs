using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types.Metadata;

/// <summary>
/// Holds which authorization requirements are used across the composed schema and the distinct
/// policy names they reference. Stored as a schema feature.
/// </summary>
internal sealed class FusionAuthorizationUsage(
    FusionAuthorizationUsageFlags flags,
    ImmutableArray<string> policyNames)
{
    /// <summary>
    /// Gets a value indicating whether any member requires an authenticated user.
    /// </summary>
    public bool UsesAuthenticated => (flags & FusionAuthorizationUsageFlags.Authenticated) != 0;

    /// <summary>
    /// Gets a value indicating whether any member requires scopes.
    /// </summary>
    public bool UsesScopes => (flags & FusionAuthorizationUsageFlags.Scopes) != 0;

    /// <summary>
    /// Gets a value indicating whether any member requires policies.
    /// </summary>
    public bool UsesPolicies => (flags & FusionAuthorizationUsageFlags.Policies) != 0;

    /// <summary>
    /// Gets a value indicating whether any member carries an authorization requirement.
    /// </summary>
    public bool IsUsed => flags != FusionAuthorizationUsageFlags.None;

    /// <summary>
    /// Gets the distinct policy names in ordinal order.
    /// </summary>
    public ImmutableArray<string> PolicyNames { get; } = policyNames;
}
