namespace HotChocolate.Types;

/// <summary>
/// Defines the consequence that applies when a policy expression denies access.
/// </summary>
public enum PolicyDenialBehavior
{
    /// <summary>
    /// The guarded value is set to null without an error.
    /// </summary>
    Null = 0,

    /// <summary>
    /// The guarded value is set to null and an authorization error is added.
    /// </summary>
    Error = 1,

    /// <summary>
    /// The request is terminated.
    /// </summary>
    Abort = 2
}
