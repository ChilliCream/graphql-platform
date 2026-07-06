namespace Mocha;

/// <summary>
/// Provides read-only access to the global messaging options that govern bus-wide defaults.
/// </summary>
public interface IReadOnlyMessagingOptions
{
    /// <summary>
    /// Gets the default content type used for message serialization when no explicit content type
    /// is specified on a message type.
    /// </summary>
    MessageContentType DefaultContentType { get; }

    /// <summary>
    /// Gets a value indicating whether all message types must be explicitly registered at startup.
    /// When <c>true</c>, runtime auto-registration of unknown types is disabled.
    /// </summary>
    bool IsAotCompatible { get; }
}
