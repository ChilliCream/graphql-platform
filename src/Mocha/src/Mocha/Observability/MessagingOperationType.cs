namespace Mocha;

/// <summary>
/// Defines the types of messaging operations for OpenTelemetry instrumentation, following the semantic conventions for messaging spans.
/// </summary>
public enum MessagingOperationType
{
    /// <summary>
    /// A message send operation (producer).
    /// </summary>
    Send,

    /// <summary>
    /// A message creation operation (producer). The message is created and handed to a later send,
    /// which carries the creation context into the message.
    /// </summary>
    Create,

    /// <summary>
    /// A message receive operation (consumer pull).
    /// </summary>
    Receive,

    /// <summary>
    /// A message processing operation (consumer handling).
    /// </summary>
    Process,

    /// <summary>
    /// A message settlement operation (acknowledge, reject, or dead-letter).
    /// </summary>
    Settle
}
