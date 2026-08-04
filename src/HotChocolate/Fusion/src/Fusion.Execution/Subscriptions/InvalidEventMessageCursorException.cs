namespace HotChocolate.Fusion.Subscriptions;

/// <summary>
/// Represents a resume cursor that cannot be used by an event stream broker.
/// </summary>
public sealed class InvalidEventMessageCursorException : Exception
{
    public const string DefaultMessage = "The cursor is invalid.";

    public InvalidEventMessageCursorException()
        : base(DefaultMessage)
    {
    }

    public InvalidEventMessageCursorException(Exception innerException)
        : base(DefaultMessage, innerException)
    {
    }
}
