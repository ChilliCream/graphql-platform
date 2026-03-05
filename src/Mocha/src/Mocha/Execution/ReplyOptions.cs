using System.Text.Json.Serialization;

namespace Mocha;

/// <summary>
/// Options controlling the behavior of a reply operation, including the destination address and correlation metadata.
/// </summary>
public readonly struct ReplyOptions
{
    /// <summary>
    /// Gets the correlation identifier linking this reply to the original request.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the conversation identifier for the reply.
    /// </summary>
    public string? ConversationId { get; init; }

    /// <summary>
    /// Gets the destination address where the reply should be sent.
    /// </summary>
    public Uri ReplyAddress { get; init; }

    /// <summary>
    /// Gets custom headers to include with the reply message, or <c>null</c> if none.
    /// </summary>
    public Dictionary<string, object?>? Headers { get; init; }

    /// <summary>
    /// Gets the default reply options with no overrides.
    /// </summary>
    public static readonly ReplyOptions Default;
}
