namespace HotChocolate.Subscriptions;

/// <summary>
/// Specifies the behavior to use when writing to a topic buffer that is already full.
/// </summary>
public enum TopicBufferFullMode
{
    /// <summary>
    /// Remove and ignore the newest item in the topic channel in order to make room for
    /// the item being written.
    /// </summary>
    DropNewest,

    /// <summary>
    /// Remove and ignore the oldest item in the topic channel in order to make room for
    /// the item being written.
    /// </summary>
    DropOldest,

    /// <summary>
    /// Drop the item being written.
    /// </summary>
    DropWrite
}
