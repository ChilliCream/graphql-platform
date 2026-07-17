using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Scheduling;
using Mocha.Threading;

namespace Mocha.Transport.InMemory.Scheduling;

/// <summary>
/// A hosted background worker that drains due entries from the in-memory scheduled message store and
/// dispatches them through the normal endpoint pipeline. It sleeps on the store's dedicated signal
/// until the next entry is due.
/// </summary>
internal sealed class InMemoryScheduledMessageWorker(
    IServiceProvider services,
    IMessagingRuntime runtime,
    IMessagingPools pools,
    InMemoryTransportScheduledMessageStore store,
    TimeProvider timeProvider,
    ILogger<InMemoryScheduledMessageWorker> logger)
    : IHostedService, IAsyncDisposable
{
    private readonly ObjectPool<DispatchContext> _contextPool = pools.DispatchContext;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private ContinuousTask? _task;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_task is not null)
            {
                return Task.CompletedTask;
            }

            _task = new ContinuousTask(ProcessAsync);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        ContinuousTask? task;

        lock (_lock)
        {
            task = _task;
            _task = null;
        }

        if (task is not null)
        {
            await task.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var now = timeProvider.GetUtcNow();

                if (store.TryTakeDue(now, out var entry))
                {
                    await DispatchAsync(entry, cancellationToken);

                    continue;
                }

                var wakeTime = store.NextDueTime() ?? DateTimeOffset.MaxValue;

                await store.Signal.WaitUntilAsync(wakeTime, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
        }
    }

    private async Task DispatchAsync(ScheduledEntry entry, CancellationToken cancellationToken)
    {
        var envelope = entry.Envelope;
        var messageType = GetMessageType(envelope.MessageType);
        var isReply = envelope.Headers?.IsReply() ?? false;
        var endpoint = isReply
            ? GetReplyDispatchEndpoint(envelope.DestinationAddress)
            : GetDispatchEndpoint(envelope.DestinationAddress);

        if (messageType is null || endpoint is null)
        {
            logger.CouldNotResolveScheduledMessage(entry.Id);

            return;
        }

        Activity? activity = null;
        var traceparent = envelope.Headers?.Get(MessageHeaders.Traceparent);

        if (!string.IsNullOrEmpty(traceparent))
        {
            var tracestate = envelope.Headers?.Get(MessageHeaders.Tracestate);
            if (ActivityContext.TryParse(traceparent, tracestate, out var parentContext))
            {
                activity = OpenTelemetry.Source.CreateActivity(
                    "scheduler send",
                    ActivityKind.Client,
                    parentContext);

                activity?.SetMessageId(envelope.MessageId);

                activity?.Start();
            }
        }

        var context = _contextPool.Get();
        try
        {
            await using var scope = services.CreateAsyncScope();

            context.Initialize(scope.ServiceProvider, endpoint, runtime, messageType, cancellationToken);
            context.SkipScheduler();
            context.SkipOutbox();
            context.Envelope = envelope;

            await endpoint.ExecuteAsync(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.ScheduledMessageDispatchFailed(entry.Id, ex);
        }
        finally
        {
            _contextPool.Return(context);
            activity?.Dispose();
        }
    }

    private MessageType? GetMessageType(string? messageType)
    {
        try
        {
            return messageType is null ? null : runtime.Messages.GetMessageType(messageType);
        }
        catch
        {
            return null;
        }
    }

    private DispatchEndpoint? GetReplyDispatchEndpoint(string? destinationAddress)
    {
        try
        {
            if (!Uri.TryCreate(destinationAddress, UriKind.Absolute, out var uri))
            {
                return null;
            }

            return runtime.GetTransport(uri)?.ReplyDispatchEndpoint;
        }
        catch
        {
            return null;
        }
    }

    private DispatchEndpoint? GetDispatchEndpoint(string? destinationAddress)
    {
        try
        {
            if (destinationAddress is null || !Uri.TryCreate(destinationAddress, UriKind.Absolute, out var uri))
            {
                return null;
            }

            return runtime.GetDispatchEndpoint(uri);
        }
        catch
        {
            return null;
        }
    }
}

internal static partial class InMemorySchedulerLogs
{
    [LoggerMessage(
        1,
        LogLevel.Critical,
        "Could not resolve the message type or endpoint for scheduled message {Id}. Message dropped.")]
    public static partial void CouldNotResolveScheduledMessage(this ILogger logger, Guid id);

    [LoggerMessage(2, LogLevel.Error, "Failed to dispatch scheduled message {Id}. Message dropped.")]
    public static partial void ScheduledMessageDispatchFailed(this ILogger logger, Guid id, Exception exception);
}
