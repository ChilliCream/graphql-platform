namespace HotChocolate.Fusion;

/// <summary>
/// Defines the names of the values that can be used with the fusion resolver kind enum.
/// </summary>
internal static class FusionEnumValueNames
{
    /// <summary>
    /// Gets the name of the query resolver kind.
    /// </summary>
    public const string Query = "QUERY";

    /// <summary>
    /// Gets the name of the batch resolver kind.
    /// </summary>
    public const string Batch = "BATCH";

    /// <summary>
    /// Gets the name of the batch by key resolver kind.
    /// </summary>
    public const string BatchByKey = "BATCH_BY_KEY";

    /// <summary>
    /// Gets the name of the subscription resolver kind.
    /// </summary>
    public const string Subscription = "SUBSCRIPTION";
}
