using static HotChocolate.Subscriptions.Properties.Resources;

namespace HotChocolate.Subscriptions;

/// <summary>
/// The message envelope into which event messages are wrapped.
/// </summary>
/// <typeparam name="TBody">
/// The type of the message body.
/// </typeparam>
public readonly struct MessageEnvelope<TBody>
{
    /// <summary>
    /// Initializes a new instance of <see cref="MessageEnvelope{TBody}"/>
    /// </summary>
    /// <param name="body">The message body.</param>
    /// <param name="kind">The message kind.</param>
    public MessageEnvelope(TBody? body = default, MessageKind kind = MessageKind.Default)
    {
        Body = body;
        Kind = kind;

        if (kind is MessageKind.Default && body is null)
        {
            throw new ArgumentException(
                MessageEnvelope_DefaultMessage_NeedsBody,
                nameof(body));
        }

        if(kind is MessageKind.Completed)
        {
            Body = default;
        }
    }

    /// <summary>
    /// Gets the message body.
    /// </summary>
    public TBody? Body { get; }

    /// <summary>
    /// Gets the message kind.
    /// </summary>
    public MessageKind Kind { get; }
}
