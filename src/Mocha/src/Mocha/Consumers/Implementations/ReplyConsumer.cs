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
    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor.Name("Reply");
    }

    protected override void OnAfterInitialize(IMessagingSetupContext context)
    {
        base.OnAfterInitialize(context);
    }

    protected override ValueTask ConsumeAsync(IConsumeContext context)
    {
        if (context.CorrelationId is not { } correlationId)
        {
            // TODO logs!
            // Replies without correlation cannot be matched to a pending request promise.
            return default;
        }

        try
        {
            var message = context.GetMessage();

            if (message is null)
            {
                throw ThrowHelper.ResponseBodyNotSet();
            }

            if (message is NotAcknowledgedEvent failure)
            {
                // Fault replies complete the pending request with a remote exception.
                responseManager.SetException(
                    correlationId,
                    new RemoteErrorException(
                        failure.ErrorCode,
                        failure.ErrorMessage,
                        failure.MessageId,
                        failure.CorrelationId));
            }
            else if (!responseManager.CompletePromise(context.CorrelationId, message))
            {
                // A late/unknown reply indicates there is no active waiter for this correlation id.
                throw ThrowHelper.PromiseNotFound();
            }
        }
        catch (Exception ex)
        {
            // TODO logs!
            responseManager.SetException(correlationId, ex);
        }

        return default;
    }
}
