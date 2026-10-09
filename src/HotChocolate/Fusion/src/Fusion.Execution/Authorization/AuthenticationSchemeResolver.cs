using System.Collections.Immutable;

namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Resolves the authentication schemes the gateway authenticates against from the
/// <see cref="FusionAuthorizationOptions"/> and the schemes the host has registered.
/// </summary>
internal sealed class AuthenticationSchemeResolver
{
    private readonly FusionAuthorizationOptions _options;
    private readonly IAuthenticationSchemeLookup? _lookup;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthenticationSchemeResolver"/>.
    /// </summary>
    /// <param name="options">
    /// The authorization options.
    /// </param>
    /// <param name="lookup">
    /// The registered schemes, or <c>null</c> if the host exposes none, which resolves to no registered schemes.
    /// </param>
    public AuthenticationSchemeResolver(
        FusionAuthorizationOptions options,
        IAuthenticationSchemeLookup? lookup)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _lookup = lookup;
    }

    /// <summary>
    /// Gets a value indicating whether the host exposes its registered authentication schemes.
    /// </summary>
    public bool HasLookup => _lookup is not null;

    /// <summary>
    /// Gets the names of the registered authentication schemes.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that signals that the operation was aborted.
    /// </param>
    public ValueTask<ImmutableArray<string>> GetRegisteredAsync(CancellationToken cancellationToken)
        => _lookup is null
            ? new ValueTask<ImmutableArray<string>>([])
            : _lookup.GetSchemeNamesAsync(cancellationToken);

    /// <summary>
    /// Gets the value of the <c>WWW-Authenticate</c> header that advertises the HTTP authentication schemes
    /// of the selected registrations, deduplicated and in ordinal order, or <c>null</c> if none contributes.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that signals that the operation was aborted.
    /// </param>
    public async ValueTask<string?> GetChallengeAsync(CancellationToken cancellationToken)
    {
        if (_lookup is null)
        {
            return null;
        }

        var registered = await GetRegisteredAsync(cancellationToken).ConfigureAwait(false);
        var selected = _options.Schemes ?? registered;
        var challenges = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var schemeName in selected)
        {
            if (!registered.Contains(schemeName))
            {
                continue;
            }

            var challenge = await _lookup.GetChallengeAsync(schemeName, cancellationToken).ConfigureAwait(false);

            if (challenge is not null || _options.SchemeChallenges.TryGetValue(schemeName, out challenge))
            {
                challenges.Add(challenge);
            }
        }

        return challenges.Count == 0 ? null : string.Join(", ", challenges);
    }
}
