namespace Mocha.Mediator;

/// <summary>
/// OpenTelemetry semantic convention attribute names for messaging systems.
/// </summary>
/// <remarks>
/// See https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/
/// </remarks>
internal static class SemanticConventions
{
    // Messaging attributes
    public const string MessagingSystem = "messaging.system";
    public const string MessagingOperationType = "messaging.operation.type";
    public const string MessagingMessageType = "messaging.message.type";

    // Exception attributes
    public const string ExceptionEventName = "exception";
    public const string ExceptionType = "exception.type";
    public const string ExceptionMessage = "exception.message";

    // Messaging system identifier
    public const string MessagingSystemValue = "mocha.mediator";

    // Operation type values
    public const string OperationTypeSend = "send";
    public const string OperationTypePublish = "publish";
}
