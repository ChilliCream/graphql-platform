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
            var envelope = context.Envelope;

            var fault = FaultInfo.From(Guid.NewGuid(), provider.GetUtcNow(), ex);

            // A requester expecting a reply should get an explicit negative acknowledgement first.
            if (envelope?.ResponseAddress is { } responseAddress
                && Uri.TryCreate(responseAddress, UriKind.Absolute, out var responseAddressUri))
            {
                var responseTransport = context.Runtime.GetTransport(responseAddressUri);
                if (responseTransport?.HasCapability(MessagingTransportCapabilities.RequestReply) == true)
                {
                    await ReplyToSenderAsync(context, responseAddressUri, responseTransport, envelope, fault);
                }
                else
                {
                    await SendToErrorEndpointAsync(context, envelope, fault);
                }
            }
            else
            {
                await SendToErrorEndpointAsync(context, envelope, fault);
            }

            feature.MessageConsumed = true;
        }
    }

    private async ValueTask ReplyToSenderAsync(
        IReceiveContext context,
        Uri responseAddress,
        MessagingTransport responseTransport,
        MessageEnvelope envelope,
        FaultInfo fault)
    {
        var replyEndpoint = responseTransport.ReplyDispatchEndpoint;
        if (replyEndpoint is null)
        {
            throw ThrowHelper.NoReplyEndpointFound(responseAddress.ToString());
        }

        var messageType = context.Runtime.GetMessageType(typeof(NotAcknowledgedEvent));

        var dispatchContext = pools.DispatchContext.Get();
        try
        {
            dispatchContext.CorrelationId = envelope?.CorrelationId;
            dispatchContext.ConversationId = envelope?.ConversationId;
            dispatchContext.DestinationAddress = responseAddress;
            dispatchContext.SourceAddress = replyEndpoint.Address;

            dispatchContext.Initialize(
                context.Services,
                replyEndpoint,
                context.Runtime,
                messageType,
                context.CancellationToken);

            var exceptionType = fault.Exceptions.FirstOrDefault()?.ExceptionType;
            var message = $"The message faulted with an exception of type {exceptionType}";

            dispatchContext.Headers.SetMessageKind(MessageKind.Fault);

            dispatchContext.Message = new NotAcknowledgedEvent(
                envelope!.CorrelationId,
                envelope.MessageId,
                fault.ErrorCode,
                message);

            await replyEndpoint.ExecuteAsync(dispatchContext);
        }
        finally
        {
            pools.DispatchContext.Return(dispatchContext);
        }
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

            dispatchContext.Envelope = CreateErrorEnvelope(errorEndpoint, envelope, fault);

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
                var errorEndpoint = context.Endpoint.ErrorEndpoint;
                var pools = context.Services.GetRequiredService<IMessagingPools>();
                var timeProvider = context.Services.GetRequiredService<TimeProvider>();
                var middleware = new ReceiveFaultMiddleware(timeProvider, errorEndpoint, pools);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Fault");

    private static MessageEnvelope? CreateErrorEnvelope(
        DispatchEndpoint endpoint,
        MessageEnvelope? envelope,
        FaultInfo fault)
    {
        if (envelope is null)
        {
            return null;
        }

        var includeFaultDetails =
            endpoint.Transport.HasCapability(MessagingTransportCapabilities.RequestReply);
        var headers = CreateFaultHeaders(envelope.Headers, includeFaultDetails);
        headers.AddFault(fault, includeFaultDetails);

        return new MessageEnvelope(envelope)
        {
            ResponseAddress = null,
            ScheduledTime = null,
            Headers = headers
        };
    }

    private static IHeaders CreateFaultHeaders(IHeaders? source, bool includeDetails)
    {
        if (source is null)
        {
            return new Headers();
        }

        if (includeDetails)
        {
            return new Headers(source);
        }

        return new Headers(source.Where(static header =>
            header.Key != MessageHeaders.Fault.ExceptionType.Key
            && header.Key != MessageHeaders.Fault.Message.Key
            && header.Key != MessageHeaders.Fault.StackTrace.Key));
    }
}

file static class Extensions
{
    /// <summary>
    /// Maps fault metadata to transport headers so downstream tooling can inspect failures without
    /// deserializing a message body.
    /// </summary>
    public static void AddFault(this IHeaders headers, FaultInfo fault, bool includeDetails)
    {
        headers.SetMessageKind(MessageKind.Fault);

        if (fault.Exceptions.FirstOrDefault() is { } exception)
        {
            if (includeDetails)
            {
                headers.Set(MessageHeaders.Fault.ExceptionType, exception.ExceptionType);
                headers.Set(MessageHeaders.Fault.Message, exception.Message);
                headers.Set(MessageHeaders.Fault.StackTrace, exception.StackTrace);
            }
            else
            {
                headers.Set(MessageHeaders.Fault.ExceptionType, "MessageHandlerFault");
                headers.Set(MessageHeaders.Fault.Message, "The message faulted while being processed.");
            }
        }

        headers.Set(MessageHeaders.Fault.Timestamp, fault.Timestamp.ToString("O"));
    }
}
