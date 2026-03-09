namespace Mocha;

/// <summary>
/// Defines standard message kind identifiers used in message headers.
/// </summary>
public static class MessageKind
{
    /// <summary>
    /// An initial message expecting a reply.
    /// </summary>
    public const string Request = "request";

    /// <summary>
    /// A response to a previous request.
    /// </summary>
    public const string Reply = "reply";

    /// <summary>
    /// A message instructing an action to be performed. No response expected.
    /// </summary>
    public const string Send = "send";

    /// <summary>
    /// A message published to a potentially multiple subscribers.
    /// </summary>
    public const string Publish = "publish";

    /// <summary>
    /// Acknowledgment that a message was received and/or processed successfully.
    /// </summary>
    public const string Ack = "ack";

    /// <summary>
    /// Negative acknowledgment. Message was received but rejected or could not be processed.
    /// </summary>
    public const string Nack = "nack";

    /// <summary>
    /// Indicates a failure or error occurred while processing a message.
    /// </summary>
    public const string Fault = "fault";

    /// <summary>
    /// Indicates a request did not receive a response within the expected time frame.
    /// </summary>
    public const string Timeout = "timeout";

    /// <summary>
    /// A connectivity check request.
    /// </summary>
    public const string Ping = "ping";

    /// <summary>
    /// An internal system command (e.g., shutdown, pause, reconfigure).
    /// </summary>
    public const string Control = "control";

    /// <summary>
    /// A message used for debugging, tracing, or observability purposes.
    /// </summary>
    public const string Trace = "trace";

    /// <summary>
    /// A request to subscribe to a channel or topic.
    /// </summary>
    public const string Subscribe = "subscribe";
}
