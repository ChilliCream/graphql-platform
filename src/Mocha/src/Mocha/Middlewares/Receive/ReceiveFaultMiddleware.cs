using Microsoft.Extensions.DependencyInjection;
using Mocha.Configuration.Faults;
using Mocha.Events;
using Mocha.Features;

namespace Mocha.Middlewares;

/// <summary>
/// Converts receive-pipeline exceptions into explicit fault signals that preserve correlation to
/// the original message.
/// </summary>
/// <remarks>
/// The middleware follows a two-path failure contract:
/// request/response flows receive a direct negative acknowledgement on the response address, while
/// non-request flows are forwarded to the error endpoint with fault metadata in headers.
/// This keeps failure observable for both callers and operations, similar to fault-event + error
/// queue patterns used in broker-centric systems.
/// Without this middleware, callers often only see timeouts, and operators lose structured error
/// context tied to the original envelope.
/// </remarks>
internal sealed class ReceiveFaultMiddleware(
    TimeProvider provider,
    DispatchEndpoint? errorEndpoint,
    IMessagingPools pools)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var fault = FaultInfo.From(Guid.NewGuid(), provider.GetUtcNow(), ex);

            // A requester expecting a reply should get an explicit negative acknowledgement first.
            if (context.TryCreateResponseOptions(out var options))
            {
                await ReplyToSenderAsync(context, options, fault);
            }
            else
            {
                await SendToErrorEndpointAsync(context, context.Envelope, fault);
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

        // TODO unfortunately this can fail too.. so we need a way around this
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
                var middleware = new ReceiveFaultMiddleware(timeProvider, errorEndpoint, pools);
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
