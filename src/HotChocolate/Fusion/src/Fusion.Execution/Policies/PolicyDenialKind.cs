namespace HotChocolate.Fusion.Policies;

/// <summary>
/// The reason a selection is denied.
/// </summary>
internal enum PolicyDenialKind
{
    /// <summary>
    /// The principal of the request is not authenticated.
    /// </summary>
    Unauthenticated,

    /// <summary>
    /// The principal of the request is authenticated but the policies do not allow the selection.
    /// </summary>
    Unauthorized
}
