using System.Collections.Immutable;

namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// The built-in policy of the <c>@requiresScopes</c> directive. An entry is allowed when the
/// principal is authenticated and holds all scopes of at least one of the scope groups of the entry.
/// </summary>
public sealed class RequiresScopesPolicy : IPolicy
{
    internal const string DefaultScopeClaimType = "scope";

    private readonly string _scopeClaimType;
    private readonly ScopeClaimFormat _scopeClaimFormat;

    /// <summary>
    /// Initializes a new instance of <see cref="RequiresScopesPolicy"/> that reads the
    /// space delimited <c>scope</c> claim.
    /// </summary>
    public RequiresScopesPolicy() : this(DefaultScopeClaimType, ScopeClaimFormat.SpaceSeparated)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RequiresScopesPolicy"/>.
    /// </summary>
    /// <param name="scopeClaimType">
    /// The claim type whose values are the scopes of the principal.
    /// </param>
    /// <param name="scopeClaimFormat">
    /// How the scopes are stored in the values of the claim.
    /// </param>
    public RequiresScopesPolicy(string scopeClaimType, ScopeClaimFormat scopeClaimFormat)
    {
        ArgumentException.ThrowIfNullOrEmpty(scopeClaimType);
        _scopeClaimType = scopeClaimType;
        _scopeClaimFormat = scopeClaimFormat;
    }

    /// <inheritdoc />
    public ValueTask EvaluateAsync(
        PolicyEvaluationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.Identity?.IsAuthenticated is not true)
        {
            foreach (ref readonly var entry in context.Entries)
            {
                context.Deny(in entry);
            }

            return ValueTask.CompletedTask;
        }

        var granted = ReadScopes(context);

        foreach (ref readonly var entry in context.Entries)
        {
            if (IsSatisfied(entry.Descriptor.Scopes, granted))
            {
                context.Allow(in entry);
            }
            else
            {
                context.Deny(in entry);
            }
        }

        return ValueTask.CompletedTask;
    }

    private HashSet<string> ReadScopes(PolicyEvaluationContext context)
    {
        var granted = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claim in context.User.FindAll(_scopeClaimType))
        {
            if (_scopeClaimFormat is ScopeClaimFormat.Array)
            {
                granted.Add(claim.Value);
                continue;
            }

            foreach (var scope in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                granted.Add(scope);
            }
        }

        return granted;
    }

    private static bool IsSatisfied(
        ImmutableArray<ImmutableArray<string>> groups,
        HashSet<string> granted)
    {
        foreach (var group in groups)
        {
            var satisfied = true;

            foreach (var scope in group)
            {
                if (!granted.Contains(scope))
                {
                    satisfied = false;
                    break;
                }
            }

            if (satisfied)
            {
                return true;
            }
        }

        return false;
    }
}
