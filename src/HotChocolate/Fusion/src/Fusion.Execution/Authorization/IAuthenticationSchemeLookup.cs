using System.Collections.Immutable;

namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Provides the authentication schemes the host has registered and the challenges their handlers advertise.
/// </summary>
internal interface IAuthenticationSchemeLookup
{
    /// <summary>
    /// Gets the names of all registered authentication schemes.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that signals that the operation was aborted.
    /// </param>
    ValueTask<ImmutableArray<string>> GetSchemeNamesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the HTTP authentication scheme that the handler of the named registration advertises by protocol,
    /// or <c>null</c> if the handler does not fix one.
    /// </summary>
    /// <param name="schemeName">
    /// The name of a registered authentication scheme.
    /// </param>
    /// <param name="cancellationToken">
    /// The token that signals that the operation was aborted.
    /// </param>
    ValueTask<string?> GetChallengeAsync(string schemeName, CancellationToken cancellationToken);
}
