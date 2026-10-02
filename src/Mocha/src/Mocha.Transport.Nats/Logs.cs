using Microsoft.Extensions.Logging;

namespace Mocha.Transport.Nats;

internal static partial class Logs
{
    [LoggerMessage(
        LogLevel.Warning,
        "The NATS connection uses SubPendingChannelFullMode '{FullMode}', so a subscriber falling "
        + "more than {Capacity} messages behind drops messages instead of applying back pressure. "
        + "JetStream traffic is unaffected because pull consumers are bound by MaxAckPending, but "
        + "request/reply responses can be lost. Set SubPendingChannelFullMode to Wait on NatsOpts "
        + "to avoid this.")]
    public static partial void LossySubscriptionDefaults(
        this ILogger logger,
        string fullMode,
        int capacity);

    [LoggerMessage(
        LogLevel.Information,
        "Stream '{StreamName}' was not created because another service already captures its "
        + "subjects ({Reason}). Those subjects resolve to the stream that owns them.")]
    public static partial void YieldedConventionStream(
        this ILogger logger,
        string streamName,
        string? reason);

    [LoggerMessage(
        LogLevel.Warning,
        "The derived {Kind} name '{Name}' is longer than the recommended {Limit} characters. NATS "
        + "uses these names as storage directory names. Shorten the service name, or set the name "
        + "explicitly.")]
    public static partial void UnwieldyName(
        this ILogger logger,
        string kind,
        string name,
        int limit);

    [LoggerMessage(
        LogLevel.Error,
        "The consume loop for endpoint '{EndpointName}' failed and will restart in {Delay} seconds.")]
    public static partial void ConsumeLoopFailed(
        this ILogger logger,
        Exception exception,
        string endpointName,
        double delay);

    [LoggerMessage(
        LogLevel.Error,
        "Endpoint '{EndpointName}' has no stream to read consumer '{ConsumerName}' from and will not "
        + "receive messages.")]
    public static partial void EndpointHasNoStream(
        this ILogger logger,
        string endpointName,
        string? consumerName);

    [LoggerMessage(
        LogLevel.Warning,
        "Reporting acknowledgement progress failed. The handler continues, but its message is "
        + "redelivered once the acknowledgement deadline expires.")]
    public static partial void AckProgressFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        LogLevel.Warning,
        "Negatively acknowledging a failed message did not reach the server, so it is redelivered "
        + "once its acknowledgement deadline expires rather than immediately.")]
    public static partial void SettlementFailed(this ILogger logger, Exception exception);
}
