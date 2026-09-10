namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Describes how a batched response relates to the items of the request it answers.
/// </summary>
internal enum AliasBatchSplitStatus
{
    /// <summary>
    /// The response was split, and every item received its own result.
    /// </summary>
    Split,

    /// <summary>
    /// The response carries an outcome that applies to every item, so the whole request failed.
    /// </summary>
    Broadcast,

    /// <summary>
    /// The response does not follow the shape a batched request asks for.
    /// </summary>
    Invalid
}
