using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Configuration.Faults;
using Mocha.Events;
using Mocha.Features;

namespace Mocha.Middlewares;

/// <summary>
/// Converts receive-pipeline exceptions into explicit fault signals that preserve correlation to
/// the original message.
/// </summary>
/// <remarks>
/// Faults are sent to the fault address when present, otherwise to the response address. Messages
/// with neither address are forwarded to the error endpoint with fault metadata in headers.
/// </remarks>
internal sealed class ReceiveFaultMiddleware(
    TimeProvider provider,
    DispatchEndpoint? errorEndpoint,
    IMessagingPools pools,
    ILogger<ReceiveFaultMiddleware> logger)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();

        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.CancellationToken.IsCancellationRequested)
        {
            var fault = FaultInfo.From(Guid.NewGuid(), provider.GetUtcNow(), ex);
            var reply = context.TryCreateFaultOptions(out var options)
                || context.TryCreateResponseOptions(out options);
            var destination = reply
                ? options.ReplyAddress
                : errorEndpoint?.Destination?.Address ?? errorEndpoint?.Address;
            using var logScope = logger.BeginMessageScope(context, LogLevel.Error);
            logger.MessageFaulted(ex, context.MessageId, fault.Id, destination?.ToString());
            try
            {
                if (reply)
                {
                    await ReplyToSenderAsync(context, options, fault);
                }
                else
                {
                    await SendToErrorEndpointAsync(context, context.Envelope, fault);
                }

                if (destination is not null)
                {
                    logger.FaultDelivered(context.MessageId, fault.Id, destination);
                }
            }
            catch (Exception deliveryException)
            {
                logger.FaultDeliveryFailed(deliveryException, context.MessageId, fault.Id, destination?.ToString());
                throw;
            }

            feature.MessageConsumed = true;
        }
    }

    private static async ValueTask ReplyToSenderAsync(
        IReceiveContext context,
        ReplyOptions options,
        FaultInfo fault)
    {
        var exceptionType = fault.Exceptions.FirstOrDefault()?.ExceptionType;

        var notAcknowledged = new NotAcknowledgedEvent(
            context.CorrelationId,
            context.MessageId,
            fault.ErrorCode,
            $"The message faulted with an exception of type {exceptionType}");

        var bus = context.Services.GetRequiredService<IMessageBus>();

        await bus.ReplyAsync(
            notAcknowledged,
            options with { MessageKind = MessageKind.Fault },
            context.CancellationToken);
    }

    private async ValueTask SendToErrorEndpointAsync(
        IReceiveContext context,
        MessageEnvelope? envelope,
        FaultInfo fault)
    {
        if (errorEndpoint is null)
        {
            return;
        }

        var dispatchContext = pools.DispatchContext.Get();
        try
        {
            dispatchContext.Initialize(
                context.Services,
                errorEndpoint,
                context.Runtime,
                context.MessageType,
                context.CancellationToken);

            dispatchContext.Envelope = envelope;
            envelope?.Headers?.AddFault(fault);

            await errorEndpoint.ExecuteAsync(dispatchContext);
        }
        finally
        {
            pools.DispatchContext.Return(dispatchContext);
        }
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var errorEndpoint = context.Endpoint.Features.Get<ReceiveFaultEndpointFeature>()?.Endpoint;
                var pools = context.Services.GetRequiredService<IMessagingPools>();
                var timeProvider = context.Services.GetRequiredService<TimeProvider>();
                var middleware = new ReceiveFaultMiddleware(
                    timeProvider, errorEndpoint, pools,
                    context.Services.GetRequiredService<ILogger<ReceiveFaultMiddleware>>());
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Fault");
}

file static class Extensions
{
    /// <summary>
    /// Maps fault metadata to transport headers so downstream tooling can inspect failures without
    /// deserializing a message body.
    /// </summary>
    public static void AddFault(this IHeaders headers, FaultInfo fault)
    {
        headers.SetMessageKind(MessageKind.Fault);

        if (fault.Exceptions.FirstOrDefault() is { } exception)
        {
            headers.Set(MessageHeaders.Fault.ExceptionType, exception.ExceptionType);
            headers.Set(MessageHeaders.Fault.Message, exception.Message);
            headers.Set(MessageHeaders.Fault.StackTrace, exception.StackTrace);
        }

        headers.Set(MessageHeaders.Fault.Timestamp, fault.Timestamp.ToString("O"));
    }
}
