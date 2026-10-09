namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Specifies how a denied selection is reported to the client.
/// </summary>
public enum DenyHandling
{
    /// <summary>
    /// A denied selection resolves to <c>null</c> without an error.
    /// </summary>
    Null,

    /// <summary>
    /// A denied selection resolves to <c>null</c> and produces a field error.
    /// </summary>
    Error
}
