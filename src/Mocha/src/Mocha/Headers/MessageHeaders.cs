namespace Mocha;

/// <summary>
/// Defines the well-known message header keys used for distributed tracing, message classification, and fault information.
/// </summary>
internal static class MessageHeaders
{
    /// <summary>
    /// The W3C Trace Context <c>traceparent</c> header (version-traceId-spanId-traceFlags).
    /// </summary>
    public static readonly ContextDataKey<string?> Traceparent = new("traceparent");

    /// <summary>
    /// The W3C Trace Context <c>tracestate</c> header carrying vendor-specific trace data.
    /// </summary>
    public static readonly ContextDataKey<string?> Tracestate = new("tracestate");

    /// <summary>
    /// Indicates the kind of message <see cref="MessageKind"/> it is.
    /// </summary>
    public static readonly ContextDataKey<string?> MessageKind = new("message-kind");

    /// <summary>
    /// Defines header keys for fault information attached to messages that failed processing.
    /// </summary>
    public static class Fault
    {
        /// <summary>
        /// The fully qualified type name of the exception that caused the fault.
        /// </summary>
        public static readonly ContextDataKey<string?> ExceptionType = new("fault-exception-type");

        /// <summary>
        /// The exception message describing the fault.
        /// </summary>
        public static readonly ContextDataKey<string?> Message = new("fault-message");

        /// <summary>
        /// The stack trace of the exception that caused the fault.
        /// </summary>
        public static readonly ContextDataKey<string?> StackTrace = new("fault-stack-trace");

        /// <summary>
        /// The timestamp when the fault occurred.
        /// </summary>
        public static readonly ContextDataKey<string?> Timestamp = new("fault-timestamp");
    }
}
