namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Specifies how the scopes of a principal are stored in its scope claim.
/// </summary>
public enum ScopeClaimFormat
{
    /// <summary>
    /// Every claim value holds space delimited scopes, as defined by OAuth 2.0.
    /// </summary>
    SpaceSeparated,

    /// <summary>
    /// Every claim value holds exactly one scope.
    /// </summary>
    Array
}
