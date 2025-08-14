namespace HotChocolate.Fusion;

/// <summary>
/// Defines the names of the values that can be used with the fusion resolver kind enum.
/// </summary>
internal static class FusionEnumValueNames
{
    /// <summary>
    /// Gets the name of the fetch resolver kind.
    /// </summary>
    public const string Fetch = "FETCH";

    /// <summary>
    /// Gets the name of the batch resolver kind.
    /// </summary>
    public const string Batch = "BATCH";

    /// <summary>
    /// Gets the name of the subscribe resolver kind.
    /// </summary>
    public const string Subscribe = "SUBSCRIBE";
}
