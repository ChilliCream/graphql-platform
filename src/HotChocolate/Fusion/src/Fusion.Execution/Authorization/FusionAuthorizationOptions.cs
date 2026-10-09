using System.Collections.Immutable;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Represents the authorization options of the Fusion gateway.
/// </summary>
public sealed class FusionAuthorizationOptions
{
    private bool _isReadOnly;

    /// <summary>
    /// Gets or sets how a denied selection is reported.
    /// <see cref="Authorization.DenyHandling.Null"/> by default.
    /// </summary>
    public DenyHandling DenyHandling
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    } = DenyHandling.Null;

    /// <summary>
    /// Gets or sets which denials reject the whole request.
    /// <see cref="Authorization.RejectRequestOn.Off"/> by default.
    /// </summary>
    public RejectRequestOn RejectRequestOn
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    } = RejectRequestOn.Off;

    /// <summary>
    /// Gets or sets the names of the authentication schemes the gateway authenticates against, or
    /// <c>null</c> (the default) for every registered scheme. An empty list is a configuration error,
    /// and every listed name must be a registered scheme.
    /// </summary>
    public ImmutableArray<string>? Schemes
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value is { IsDefault: true } ? null : value;
        }
    }

    /// <summary>
    /// Gets or sets the <c>WWW-Authenticate</c> challenge per authentication scheme name for handlers whose
    /// challenge is not fixed by protocol. A scheme without an entry contributes no challenge.
    /// </summary>
    public ImmutableDictionary<string, string> SchemeChallenges
    {
        get;
        set
        {
            ExpectMutableOptions();
            ArgumentNullException.ThrowIfNull(value);

            foreach (var entry in value)
            {
                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    throw ThrowHelper.SchemeChallengeEmpty(entry.Key, nameof(value));
                }
            }

            field = value;
        }
    }
#if NET10_0_OR_GREATER
        = [];
#else
        = ImmutableDictionary<string, string>.Empty;
#endif

    /// <summary>
    /// Gets or sets the type of the claim that holds the scopes of the principal.
    /// <c>scope</c> by default.
    /// </summary>
    public string ScopeClaimName
    {
        get;
        set
        {
            ExpectMutableOptions();
            ArgumentException.ThrowIfNullOrEmpty(value);

            field = value;
        }
    } = RequiresScopesPolicy.DefaultScopeClaimType;

    /// <summary>
    /// Gets or sets how the scopes are stored in the scope claim.
    /// <see cref="Authorization.ScopeClaimFormat.SpaceSeparated"/> by default.
    /// </summary>
    public ScopeClaimFormat ScopeClaimFormat
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    } = ScopeClaimFormat.SpaceSeparated;

    /// <summary>
    /// Gets or sets whether denial errors carry the directive, policy name and required scopes.
    /// <c>false</c> by default.
    /// </summary>
    public bool EnableAttribution
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the startup validation of the authentication schemes and policy names
    /// is skipped. <c>false</c> by default.
    /// </summary>
    public bool DisableAuthorizationValidation
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    }

    internal void MakeReadOnly()
        => _isReadOnly = true;

    private void ExpectMutableOptions()
    {
        if (_isReadOnly)
        {
            throw ThrowHelper.AuthorizationOptionsAreReadOnly();
        }
    }
}
