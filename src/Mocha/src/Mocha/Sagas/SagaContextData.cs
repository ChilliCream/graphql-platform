namespace Mocha.Sagas;

internal static class SagaContextData
{
    public static ContextDataKey<string> SagaId { get; } = new("saga-id");

    public static ContextDataKey<string?> CorrelationId { get; } = new("correlation-id");

    public static ContextDataKey<string?> ReplyAddress { get; } = new("saga-reply-address");
}
