using System.Security.Claims;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// A built-in <c>fusion.scope:&lt;scope&gt;</c> policy: allows the request when
/// <see cref="IPolicyContext.User"/> carries <paramref name="scope"/> in one of the configured
/// claim types. A claim value may carry more than one scope as space-delimited text, and the
/// same claim type may occur more than once on the principal; every occurrence is checked. The
/// scope is matched ordinally, case-sensitively.
/// </summary>
internal sealed class ScopePolicy(string scope, IReadOnlyList<string> claimTypes) : IPolicy
{
    private readonly string _scope = scope;
    private readonly IReadOnlyList<string> _claimTypes = claimTypes;

    public string Name { get; } = BuiltInPolicyNames.ScopePrefix + scope;

    public PolicyRequirements Requirements => PolicyRequirements.Empty;

    public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
    {
        if (!HasScope(context.User))
        {
            context.Deny(0);
        }

        return ValueTask.CompletedTask;
    }

    private bool HasScope(ClaimsPrincipal user)
    {
        foreach (var claimType in _claimTypes)
        {
            foreach (var claim in user.Claims)
            {
                if (!claim.Type.Equals(claimType, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var value in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (value.Equals(_scope, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
