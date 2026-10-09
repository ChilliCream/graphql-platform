namespace HotChocolate.Fusion.Subscriptions;

/// <summary>
/// Represents a failure to establish the initial read positions for every assigned partition
/// when a cursor-tracking subscription starts. The subscription fails rather than start from an
/// incomplete cursor.
/// </summary>
public sealed class EventStreamSeedingException : Exception
{
    public EventStreamSeedingException(string message)
        : base(message)
    {
    }
}
