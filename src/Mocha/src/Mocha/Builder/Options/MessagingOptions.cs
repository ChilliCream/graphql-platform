namespace Mocha;

/// <summary>
/// Contains the mutable global messaging options used during bus configuration.
/// </summary>
public class MessagingOptions : IReadOnlyMessagingOptions
{
    /// <summary>
    /// Gets or sets the default content type used for message serialization. Defaults to
    /// <see cref="MessageContentType.Json"/>.
    /// </summary>
    public MessageContentType DefaultContentType { get; set; } = MessageContentType.Json;
}
