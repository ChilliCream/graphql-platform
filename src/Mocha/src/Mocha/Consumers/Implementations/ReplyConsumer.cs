using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Events;

namespace Mocha;

/// <summary>
/// Consumes reply messages and completes or faults the deferred response promise matching the
/// correlation identifier.
/// </summary>
/// <remarks>
/// This consumer is automatically registered when request-reply patterns are used. It matches
/// incoming replies to outstanding promises in the <see cref="DeferredResponseManager"/> and
/// propagates results or errors.
/// </remarks>
// TODO Not sure if this really has to be consumer. could also just be a middleware
public sealed class ReplyConsumer(DeferredResponseManager responseManager) : Consumer
{
    private ILogger<ReplyConsumer> _logger = default!;

    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor.Name("Reply");
    }

    protected override void OnAfterInitialize(IMessagingSetupContext context)
    {
        base.OnAfterInitialize(context);

        _logger = context.Services.GetRequiredService<ILogger<ReplyConsumer>>();
    }

    protected override ValueTask ConsumeAsync(IConsumeContext context)
    {
        if (context.CorrelationId is not { } correlationId)
        {
            // Dispatch always stamps a correlation id, so a reply without one came from elsewhere.
            // It cannot match a promise, and only matters when no other consumer claimed it.
            ReportUnmatchedReply(context, null);
            return default;
        }

        try
        {
            if (context.GetMessage() is not { } message)
            {
                throw ThrowHelper.ResponseBodyNotSet();
            }

            if (message is NotAcknowledgedEvent failure)
            {
                responseManager.SetException(
                    correlationId,
                    new RemoteErrorException(
                        failure.ErrorCode,
                        failure.ErrorMessage,
                        failure.MessageId,
                        failure.CorrelationId));
            }
            else if (!responseManager.CompletePromise(correlationId, message))
            {
                ReportUnmatchedReply(context, correlationId);
            }
        }
        catch (Exception ex)
        {
            // Fault the waiting requester rather than leave it to time out.
            responseManager.SetException(correlationId, ex);
            _logger.ReplyProcessingFailed(ex, correlationId, context.MessageId);
        }

        return default;
    }

    /// <summary>
    /// Reports a reply that completed no promise, distinguishing one that another consumer on the
    /// same message owns from one that nothing handled.
    /// </summary>
    private void ReportUnmatchedReply(IConsumeContext context, string? correlationId)
    {
        // A saga reply route selects the saga consumer alongside this one, so a second consumer on
        // the message means the reply is owned there and no promise was expected.
        if (context.Features.Get<ReceiveConsumerFeature>()?.Consumers is { Count: > 1 })
        {
            return;
        }

        _logger.ReplyDiscarded(correlationId, context.MessageId);
    }
}

internal static partial class ReplyConsumerLogs
{
    [LoggerMessage(
        LogLevel.Warning,
        "Discarded a reply that no pending request and no consumer claimed "
            + "(correlation id {CorrelationId}, message id {MessageId})")]
    public static partial void ReplyDiscarded(this ILogger logger, string? correlationId, string? messageId);

    [LoggerMessage(
        LogLevel.Error,
        "Failed to process a reply "
            + "(correlation id {CorrelationId}, message id {MessageId})")]
    public static partial void ReplyProcessingFailed(
        this ILogger logger,
        Exception exception,
        string correlationId,
        string? messageId);
}
