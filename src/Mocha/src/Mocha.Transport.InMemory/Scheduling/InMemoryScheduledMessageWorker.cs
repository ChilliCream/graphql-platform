using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using Mocha.Scheduling;
using Mocha.Threading;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Background service that delivers due in-memory scheduled messages. It sleeps on the scheduler
/// signal until the next message is due, then dispatches it through the normal endpoint pipeline.
/// Delivery is best-effort: a message that fails to dispatch is logged and dropped.
/// </summary>
internal sealed class InMemoryScheduledMessageWorker(
    IServiceProvider services,
    IMessagingRuntime runtime,
    IMessagingPools pools,
    ISchedulerSignal signal,
    InMemoryScheduledMessageStore store,
    TimeProvider timeProvider,
    ILogger<InMemoryScheduledMessageWorker> logger) : IHostedService, IAsyncDisposable
{
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
            _task ??= new ContinuousTask(ProcessAsync);
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
            if (store.TryTakeDue(timeProvider.GetUtcNow(), out var envelope))
            {
                await DispatchAsync(envelope, cancellationToken);
                continue;
            }

            await signal.WaitUntilAsync(store.NextDueTime() ?? DateTimeOffset.MaxValue, cancellationToken);
        }
    }

    private async Task DispatchAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        var messageType = runtime.GetMessageType(envelope.MessageType);
        var isReply = envelope.Headers?.IsReply() ?? false;
        var endpoint = isReply
            ? GetReplyEndpoint(envelope.DestinationAddress)
            : GetSendEndpoint(envelope.DestinationAddress);

        if (messageType is null || endpoint is null)
        {
            logger.CouldNotDispatchScheduledMessage(envelope.MessageId);
            return;
        }

        var context = pools.DispatchContext.Get();
        try
        {
            await using var scope = services.CreateAsyncScope();
            context.Initialize(scope.ServiceProvider, endpoint, runtime, messageType, cancellationToken);
            context.SkipScheduler();

            // Unlike the Postgres dispatcher, this worker does not call SkipOutbox(): the in-memory
            // transport is not paired with an outbox, and the re-dispatched envelope carries no
            // headers, so the outbox middleware passes through. Avoids coupling to Mocha.Outbox.
            context.Envelope = envelope;
            await endpoint.ExecuteAsync(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.ScheduledMessageDispatchFailed(envelope.MessageId, ex);
        }
        finally
        {
            pools.DispatchContext.Return(context);
        }
    }

    private DispatchEndpoint? GetSendEndpoint(string? address)
    {
        try
        {
            return Uri.TryCreate(address, UriKind.Absolute, out var uri)
                ? runtime.GetDispatchEndpoint(uri)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private DispatchEndpoint? GetReplyEndpoint(string? address)
    {
        try
        {
            return Uri.TryCreate(address, UriKind.Absolute, out var uri)
                ? runtime.GetTransport(uri)?.ReplyDispatchEndpoint
                : null;
        }
        catch
        {
            return null;
        }
    }
}

internal static partial class InMemoryScheduledMessageWorkerLog
{
    [LoggerMessage(1, LogLevel.Warning, "Could not dispatch scheduled message {MessageId}")]
    public static partial void CouldNotDispatchScheduledMessage(this ILogger logger, string? messageId);

    [LoggerMessage(2, LogLevel.Error, "Scheduled message {MessageId} dispatch failed")]
    public static partial void ScheduledMessageDispatchFailed(
        this ILogger logger,
        string? messageId,
        Exception exception);
}
