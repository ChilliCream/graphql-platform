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

    /// <summary>
    /// Gets or sets a value indicating whether all message types must be explicitly registered at
    /// startup. When <c>true</c>, runtime auto-registration of unknown types throws instead of
    /// falling back to reflection-based serialization. Defaults to <c>false</c>.
    /// </summary>
    public bool RequireExplicitMessageTypes { get; set; }
}
