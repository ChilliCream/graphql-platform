namespace HotChocolate.Fusion.Types;

/// <summary>
/// Specifies how a Fusion gateway handles entities denied by an authorization policy.
/// </summary>
public enum PolicyDenialBehavior
{
    // Ordered so a numeric max yields the total order ABORT > ERROR > NULL.
    /// <summary>
    /// Denied entities are replaced with <see langword="null"/>.
    /// </summary>
    Null = 0,

    /// <summary>
    /// Denied entities produce errors.
    /// </summary>
    Error = 1,

    /// <summary>
    /// A denied entity aborts the operation.
    /// </summary>
    Abort = 2
}
