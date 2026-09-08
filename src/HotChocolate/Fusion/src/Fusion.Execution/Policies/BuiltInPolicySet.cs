using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Builds the built-in policies a gateway provides by default: one <see cref="AuthenticatedPolicy"/>
/// or <see cref="ScopePolicy"/> instance for every built-in policy name the schema being built
/// actually references, so an unreferenced built-in never occupies a slot in a schema's policy
/// collection.
/// </summary>
internal static class BuiltInPolicySet
{
    /// <summary>
    /// The claim types <see cref="ScopePolicy"/> reads by default when no
    /// <see cref="FusionOptions.ScopeClaimTypes"/> is available.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultScopeClaimTypes = ["scope", "scp"];

    /// <summary>
    /// Creates the built-in policies for a schema, given every distinct built-in policy name
    /// (<see cref="BuiltInPolicyNames.Authenticated"/> or a <see cref="BuiltInPolicyNames.ScopePrefix"/>
    /// prefixed name) the schema's <c>@fusion__policy</c> applications reference.
    /// </summary>
    public static ImmutableArray<IPolicy> Create(
        IEnumerable<string> referencedBuiltInPolicyNames,
        IReadOnlyList<string> scopeClaimTypes)
    {
        var builder = ImmutableArray.CreateBuilder<IPolicy>();

        foreach (var name in referencedBuiltInPolicyNames)
        {
            if (name.Equals(BuiltInPolicyNames.Authenticated, StringComparison.Ordinal))
            {
                builder.Add(new AuthenticatedPolicy());
            }
            else if (name.Equals(BuiltInPolicyNames.Deny, StringComparison.Ordinal))
            {
                builder.Add(new DenyPolicy());
            }
            else if (name.StartsWith(BuiltInPolicyNames.ScopePrefix, StringComparison.Ordinal))
            {
                var scope = name[BuiltInPolicyNames.ScopePrefix.Length..];
                builder.Add(new ScopePolicy(scope, scopeClaimTypes));
            }
        }

        return builder.ToImmutable();
    }
}
