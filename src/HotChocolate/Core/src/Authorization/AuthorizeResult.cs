namespace HotChocolate.Authorization;

/// <summary>
/// Represents the authorization result.
/// </summary>
public enum AuthorizeResult
{
    /// <summary>
    /// The current session is allowed to access the resolver data.
    /// </summary>
    Allowed,

    /// <summary>
    /// The current session is not allowed to access the resolver data.
    /// </summary>
    NotAllowed,

    /// <summary>
    /// The current session is not authenticated.
    /// </summary>
    NotAuthenticated,

    /// <summary>
    /// There is no default policy configured and authorize cannot be handled.
    /// </summary>
    NoDefaultPolicy,

    /// <summary>
    /// The specified authorization policy was not found.
    /// </summary>
    PolicyNotFound
}
