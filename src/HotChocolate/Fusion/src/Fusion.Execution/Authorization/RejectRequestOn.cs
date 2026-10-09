namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Specifies which denials reject the whole request. Each level includes the levels before it.
/// </summary>
public enum RejectRequestOn
{
    /// <summary>
    /// A denial never rejects the request.
    /// </summary>
    Off,

    /// <summary>
    /// A denial because the principal is not authenticated rejects the request.
    /// </summary>
    OnUnauthenticated,

    /// <summary>
    /// Any denial rejects the request.
    /// </summary>
    OnUnauthorized
}
