using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha;

/// <summary>
/// Default implementation of <see cref="IMessageBus"/> that dispatches publish, send, request, and reply
/// operations through the configured messaging runtime and middleware pipeline.
/// </summary>
/// <remarks>
/// This class pools <see cref="DispatchContext"/> instances for each operation to reduce allocation overhead.
/// Each operation resolves the target endpoint via the runtime's router, initializes a context with the
/// appropriate message kind, and executes the endpoint's middleware pipeline.
/// </remarks>
/// <param name="runtime">The messaging runtime used to resolve message types, endpoints, and transports.</param>
/// <param name="services">The scoped service provider injected into each dispatch context.</param>
/// <param name="pools">Object pools providing reusable <see cref="DispatchContext"/> instances.</param>
/// <param name="consumeContextAccessor">Accessor for the ambient consume context used to propagate correlation IDs.</param>
public sealed class DefaultMessageBus(
    IMessagingRuntime runtime,
    IServiceProvider services,
    IMessagingPools pools,
    ConsumeContextAccessor consumeContextAccessor) : IMessageBus
{
    private readonly ObjectPool<DispatchContext> _contextPool = pools.DispatchContext;

    private readonly DeferredResponseManager _deferredResponseManager =
        runtime.Services.GetRequiredService<DeferredResponseManager>();

    /// <summary>
    /// Publishes a message to all subscribed consumers using default publish options.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="message">The message instance to publish. Must not be <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token to cancel the publish operation.</param>
    public async ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken)
    {
        await PublishAsync(message, PublishOptions.Default, cancellationToken);
    }

    /// <summary>
    /// Publishes a message to all subscribed consumers using the specified publish options.
    /// </summary>
    /// <remarks>
    /// The message is routed through the publish endpoint resolved by the runtime's router for the
    /// given message type. Custom headers and expiration time from <paramref name="options"/> are
    /// applied to the dispatch context before pipeline execution.
    /// </remarks>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="message">The message instance to publish. Must not be <see langword="null"/>.</param>
    /// <param name="options">Options controlling headers and expiration for this publish operation.</param>
    /// <param name="cancellationToken">A token to cancel the publish operation.</param>
    public async ValueTask PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken)
    {
        var messageType = runtime.GetMessageType(message!.GetType());
        var endpoint = runtime.GetPublishEndpoint(messageType);

        var context = _contextPool.Get();
        try
        {
            PropagateCorrelationIds(context);
            context.Initialize(services, endpoint, runtime, messageType, cancellationToken);
            context.Message = message;
            context.AddHeaders(options.Headers);
            context.Headers.SetMessageKind(MessageKind.Publish);
            context.ScheduledTime = options.ScheduledTime;
            context.DeliverBy = options.ExpirationTime;

            await endpoint.ExecuteAsync(context);
        }
        finally
        {
            _contextPool.Return(context);
        }
    }

    /// <summary>
    /// Sends a message to a single consumer endpoint using default send options.
    /// </summary>
    /// <typeparam name="T">The type of the message to send.</typeparam>
    /// <param name="message">The message instance to send. Must not be <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token to cancel the send operation.</param>
    public ValueTask SendAsync<T>(T message, CancellationToken cancellationToken)
    {
        return SendAsync(message, SendOptions.Default, cancellationToken);
    }

    /// <summary>
    /// Sends a message to a single consumer endpoint using the specified send options.
    /// </summary>
    /// <remarks>
    /// When <see cref="SendOptions.Endpoint"/> is set, the message is dispatched to that specific
    /// address; otherwise the runtime's router resolves the endpoint by message type. Reply and fault
    /// addresses from the options are propagated to the dispatch context.
    /// </remarks>
    /// <typeparam name="T">The type of the message to send.</typeparam>
    /// <param name="message">The message instance to send. Must not be <see langword="null"/>.</param>
    /// <param name="options">Options controlling the target endpoint, headers, reply/fault addresses, and expiration.</param>
    /// <param name="cancellationToken">A token to cancel the send operation.</param>
    public async ValueTask SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken)
    {
        var messageType = runtime.GetMessageType(message!.GetType());
        var endpoint = options.Endpoint is { } address
            ? runtime.GetDispatchEndpoint(address)
            : runtime.GetSendEndpoint(messageType);

        var replyEndpoint = options.ReplyEndpoint;
        var faultEndpoint = options.FaultEndpoint;
        var headers = options.Headers;

        var context = _contextPool.Get();
        try
        {
            PropagateCorrelationIds(context);
            context.Initialize(services, endpoint, runtime, messageType, cancellationToken);

            context.Message = message;
            context.AddHeaders(headers);
            context.Headers.SetMessageKind(MessageKind.Send);
            context.ResponseAddress = replyEndpoint;
            context.FaultAddress = faultEndpoint;
            context.ScheduledTime = options.ScheduledTime;
            context.DeliverBy = options.ExpirationTime;

            await endpoint.ExecuteAsync(context);
        }
        finally
        {
            _contextPool.Return(context);
        }
    }

    /// <summary>
    /// Sends a typed request and asynchronously waits for the corresponding response using default send options.
    /// </summary>
    /// <typeparam name="TResponse">The expected response event type.</typeparam>
    /// <param name="request">The request message that defines the expected response contract.</param>
    /// <param name="cancellationToken">A token to cancel the request/response operation.</param>
    /// <returns>The response event received from the consumer that handled the request.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the received response does not match <typeparamref name="TResponse"/>.</exception>
    public async ValueTask<TResponse> RequestAsync<TResponse>(
        IEventRequest<TResponse> request,
        CancellationToken cancellationToken)
        => await RequestAsync(request, SendOptions.Default, cancellationToken);

    /// <summary>
    /// Sends a typed request and asynchronously waits for the corresponding response using the specified send options.
    /// </summary>
    /// <remarks>
    /// A correlation ID is generated for the request, and a deferred response promise is registered
    /// so the bus can match the incoming reply. The caller blocks until the response arrives or the
    /// <paramref name="cancellationToken"/> is triggered.
    /// </remarks>
    /// <typeparam name="TResponse">The expected response event type.</typeparam>
    /// <param name="message">The request message that defines the expected response contract.</param>
    /// <param name="options">Options controlling the target endpoint, headers, reply/fault addresses, and expiration.</param>
    /// <param name="cancellationToken">A token to cancel the request/response operation.</param>
    /// <returns>The response event received from the consumer that handled the request.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the received response does not match <typeparamref name="TResponse"/>.</exception>
    public async ValueTask<TResponse> RequestAsync<TResponse>(
        IEventRequest<TResponse> message,
        SendOptions options,
        CancellationToken cancellationToken)
        => await RequestAndWaitAsync<TResponse>(message, options, cancellationToken);

    /// <summary>
    /// Sends a fire-and-forget request (no typed response) using default send options and waits for acknowledgement.
    /// </summary>
    /// <param name="request">The request message to send.</param>
    /// <param name="cancellationToken">A token to cancel the request operation.</param>
    public async ValueTask RequestAsync(object request, CancellationToken cancellationToken)
        => await RequestAsync(request, SendOptions.Default, cancellationToken);

    /// <summary>
    /// Sends a fire-and-forget request (no typed response) using the specified send options and waits for acknowledgement.
    /// </summary>
    /// <param name="message">The request message to send.</param>
    /// <param name="options">Options controlling the target endpoint, headers, reply/fault addresses, and expiration.</param>
    /// <param name="cancellationToken">A token to cancel the request operation.</param>
    public async ValueTask RequestAsync(object message, SendOptions options, CancellationToken cancellationToken)
        => await RequestAndWaitAsync<object>(message, options, cancellationToken);

    /// <summary>
    /// Sends a reply message back to the originator of a request, routed via the transport's reply dispatch endpoint.
    /// </summary>
    /// <remarks>
    /// The reply is correlated using <see cref="ReplyOptions.CorrelationId"/> and dispatched through the
    /// transport associated with <see cref="ReplyOptions.ReplyAddress"/>. The transport must expose a
    /// <see cref="MessagingTransport.ReplyDispatchEndpoint"/>; otherwise an exception is thrown.
    /// </remarks>
    /// <typeparam name="TResponse">The type of the reply message.</typeparam>
    /// <param name="response">The reply message to send back. Must not be <see langword="null"/>.</param>
    /// <param name="options">Options specifying the target endpoint, correlation ID, conversation ID, and headers.</param>
    /// <param name="cancellationToken">A token to cancel the reply operation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no transport is found for the specified endpoint address, or when the transport
    /// does not have a reply dispatch endpoint configured.
    /// </exception>
    public async ValueTask ReplyAsync<TResponse>(
        TResponse response,
        ReplyOptions options,
        CancellationToken cancellationToken)
        where TResponse : notnull
    {
        var correlationId = options.CorrelationId;
        var transport = runtime.GetTransport(options.ReplyAddress);
        if (transport is null)
        {
            throw ThrowHelper.TransportNotFoundForAddress(options.ReplyAddress.ToString());
        }

        var replyEndpoint = transport.ReplyDispatchEndpoint;
        if (replyEndpoint is null)
        {
            throw ThrowHelper.ReplyDispatchEndpointNotFound(options.ReplyAddress.ToString());
        }

        // var operationName = "reply";
        var messageType = runtime.GetMessageType(response.GetType());

        var headers = options.Headers;

        var context = _contextPool.Get();
        try
        {
            context.CorrelationId = correlationId;
            context.ConversationId = options.ConversationId;
            context.DestinationAddress = options.ReplyAddress;
            context.SourceAddress = replyEndpoint.Address;

            context.Initialize(services, replyEndpoint, runtime, messageType, cancellationToken);

            context.Message = response;

            context.AddHeaders(headers);
            context.Headers.SetMessageKind(MessageKind.Reply);

            await replyEndpoint.ExecuteAsync(context);
        }
        catch
        {
            _contextPool.Return(context);
            throw;
        }
    }

    private async ValueTask<TResponse> RequestAndWaitAsync<TResponse>(
        object message,
        SendOptions options,
        CancellationToken cancellationToken)
    {
        var requestType = runtime.GetMessageType(message.GetType());
        var endpoint = options.Endpoint is { } address
            ? runtime.GetDispatchEndpoint(address)
            : runtime.GetSendEndpoint(requestType);

        var replyEndpoint = options.ReplyEndpoint;
        var faultEndpoint = options.FaultEndpoint;
        // var operationName = $"send {endpoint}";
        var correlationId = Guid.NewGuid().ToString();

        var headers = options.Headers;

        var waitHandle = _deferredResponseManager.AddPromise(correlationId);

        var context = _contextPool.Get();
        try
        {
            PropagateCorrelationIds(context);
            context.CorrelationId = correlationId;
            context.Initialize(services, endpoint, runtime, requestType, cancellationToken);

            context.Message = message;
            context.AddHeaders(headers);
            context.Headers.SetMessageKind(MessageKind.Request);
            context.ResponseAddress = replyEndpoint ?? endpoint.Transport.ReplyReceiveEndpoint?.Source.Address;
            context.FaultAddress = faultEndpoint;
            context.ScheduledTime = options.ScheduledTime;
            context.DeliverBy = options.ExpirationTime;

            await endpoint.ExecuteAsync(context);
        }
        finally
        {
            _contextPool.Return(context);
        }

        var result = await waitHandle.Task.WaitAsync(cancellationToken);
        if (result is TResponse response)
        {
            return response;
        }

        throw ThrowHelper.UnexpectedResponseType();
    }

    /// <summary>
    /// Publishes a message scheduled for delivery at the specified time using default options.
    /// </summary>
    public async ValueTask<SchedulingResult> SchedulePublishAsync<T>(
        T message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken)
        where T : notnull
    {
        return await SchedulePublishAsync(message, scheduledTime, PublishOptions.Default, cancellationToken);
    }

    /// <summary>
    /// Publishes a message scheduled for delivery at the specified time with additional options.
    /// </summary>
    public async ValueTask<SchedulingResult> SchedulePublishAsync<T>(
        T message,
        DateTimeOffset scheduledTime,
        PublishOptions options,
        CancellationToken cancellationToken)
        where T : notnull
    {
        var messageType = runtime.GetMessageType(message!.GetType());
        var endpoint = runtime.GetPublishEndpoint(messageType);

        var context = _contextPool.Get();
        try
        {
            PropagateCorrelationIds(context);
            context.Initialize(services, endpoint, runtime, messageType, cancellationToken);
            context.Message = message;
            context.AddHeaders(options.Headers);
            context.Headers.SetMessageKind(MessageKind.Publish);
            context.ScheduledTime = scheduledTime;
            context.DeliverBy = options.ExpirationTime;

            await endpoint.ExecuteAsync(context);

            var feature = context.Features.Get<ScheduledMessageFeature>();

            return new SchedulingResult
            {
                Token = feature?.Token,
                ScheduledTime = scheduledTime,
                IsCancellable = feature?.Token is not null
            };
        }
        finally
        {
            _contextPool.Return(context);
        }
    }

    /// <summary>
    /// Sends a message scheduled for delivery at the specified time using default options.
    /// </summary>
    public async ValueTask<SchedulingResult> ScheduleSendAsync<T>(
        T message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken)
    {
        return await ScheduleSendAsync(message, scheduledTime, SendOptions.Default, cancellationToken);
    }

    /// <summary>
    /// Sends a message scheduled for delivery at the specified time with additional options.
    /// </summary>
    public async ValueTask<SchedulingResult> ScheduleSendAsync<T>(
        T message,
        DateTimeOffset scheduledTime,
        SendOptions options,
        CancellationToken cancellationToken)
    {
        var messageType = runtime.GetMessageType(message!.GetType());
        var endpoint = options.Endpoint is { } address
            ? runtime.GetDispatchEndpoint(address)
            : runtime.GetSendEndpoint(messageType);

        var replyEndpoint = options.ReplyEndpoint;
        var faultEndpoint = options.FaultEndpoint;
        var headers = options.Headers;

        var context = _contextPool.Get();
        try
        {
            PropagateCorrelationIds(context);
            context.Initialize(services, endpoint, runtime, messageType, cancellationToken);

            context.Message = message;
            context.AddHeaders(headers);
            context.Headers.SetMessageKind(MessageKind.Send);
            context.ResponseAddress = replyEndpoint;
            context.FaultAddress = faultEndpoint;
            context.ScheduledTime = scheduledTime;
            context.DeliverBy = options.ExpirationTime;

            await endpoint.ExecuteAsync(context);

            var feature = context.Features.Get<ScheduledMessageFeature>();

            return new SchedulingResult
            {
                Token = feature?.Token,
                ScheduledTime = scheduledTime,
                IsCancellable = feature?.Token is not null
            };
        }
        finally
        {
            _contextPool.Return(context);
        }
    }

    /// <summary>
    /// Cancels a previously scheduled message by forwarding the opaque token
    /// to the registered scheduling store.
    /// </summary>
    public async ValueTask<bool> CancelScheduledMessageAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var store = services.GetService<IScheduledMessageStore>();
        if (store is null)
        {
            return false;
        }

        return await store.CancelAsync(token, cancellationToken);
    }

    private void PropagateCorrelationIds(DispatchContext context)
    {
        if (consumeContextAccessor.Context is { } ambient)
        {
            context.ConversationId ??= ambient.ConversationId;
            context.CausationId ??= ambient.MessageId;
        }
    }
}

file static class Extensions
{
    public static void AddHeaders(this IDispatchContext context, Dictionary<string, object?>? headers)
    {
        if (headers is null)
        {
            return;
        }

        foreach (var header in headers)
        {
            context.Headers.Set(header.Key, header.Value);
        }
    }
}
