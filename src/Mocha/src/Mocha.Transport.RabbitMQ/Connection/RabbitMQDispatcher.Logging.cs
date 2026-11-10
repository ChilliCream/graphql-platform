using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

public sealed partial class RabbitMQDispatcher
{
    private static readonly TimeSpan s_publishWarningInterval = TimeSpan.FromSeconds(30);

    internal ValueTask WaitForPublishPhaseAsync(
        ValueTask operation, IDispatchContext context, IChannel? channel, string phase)
        => operation.IsCompletedSuccessfully || !Logger.IsEnabled(LogLevel.Warning)
            ? operation
            : new ValueTask(WaitForPublishPhaseAsync(operation.AsTask(), context, channel, phase));

    internal async ValueTask<IChannel> WaitForPublishChannelAsync(
        ValueTask<IChannel> operation, IDispatchContext context, string phase)
    {
        if (operation.IsCompletedSuccessfully || !Logger.IsEnabled(LogLevel.Warning))
        {
            return await operation;
        }

        var pending = operation.AsTask();
        await WaitForPublishPhaseAsync(pending, context, null, phase);
        return await pending;
    }

    private async Task WaitForPublishPhaseAsync(
        Task operation, IDispatchContext context, IChannel? channel, string phase)
    {
        var started = TimeProvider.GetTimestamp();
        while (!operation.IsCompleted)
        {
            try
            {
                await operation.WaitAsync(s_publishWarningInterval, TimeProvider);
                return;
            }
            catch (TimeoutException)
            {
                if (operation.IsCompleted)
                {
                    break;
                }

                using var connectionScope = channel is not null ? BeginLogScope(channel) : BeginLogScope();
                using var messageScope = Logger.BeginMessageScope(context, LogLevel.Warning);
                Logger.PublishStalled(context.Envelope?.MessageId, phase, TimeProvider.GetElapsedTime(started));
            }
        }

        await operation;
    }

    internal void LogPublishRetry(IDispatchContext context, IChannel? channel, Exception exception, int attempt, TimeSpan delay)
    {
        if (!Logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        using var connectionScope = channel is not null ? BeginLogScope(channel) : BeginLogScope();
        using var messageScope = Logger.BeginMessageScope(context, LogLevel.Debug);
        Logger.PublishFailedRetrying(exception, ConnectionName, context.Endpoint.Name, attempt, delay);
    }

    internal void LogPublishPhase(IDispatchContext context, IChannel? channel, string phase)
    {
        if (!Logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        using var connectionScope = channel is not null ? BeginLogScope(channel) : BeginLogScope();
        using var messageScope = Logger.BeginMessageScope(context, LogLevel.Debug);
        Logger.PublishWaiting(context.Envelope?.MessageId, phase);
    }

    internal void LogPublishOutcome(
        IDispatchContext context, IChannel? channel, Exception? exception, string outcome, string phase, int attempt, TimeSpan elapsed)
    {
        var level = outcome == "Failed" ? LogLevel.Error
            : elapsed >= TimeSpan.FromSeconds(10) ? LogLevel.Warning : LogLevel.Debug;
        if (!Logger.IsEnabled(level))
        {
            return;
        }

        using var connectionScope = channel is not null ? BeginLogScope(channel) : BeginLogScope();
        using var messageScope = Logger.BeginMessageScope(context, level);
        Logger.PublishFinished(exception, level, context.Envelope?.MessageId, outcome, phase, attempt, elapsed);
    }

    internal void LogReturnedQueueRepairStarted(IDispatchContext context, IChannel channel, string queueName)
    {
        using var connectionScope = BeginLogScope(channel);
        using var messageScope = Logger.BeginMessageScope(context, LogLevel.Warning);
        Logger.ReturnedQueueRepairStarted(ConnectionName, queueName);
    }

    internal void LogReturnedQueueRedeclared(IDispatchContext context, IChannel channel, string queueName)
    {
        using var connectionScope = BeginLogScope(channel);
        using var messageScope = Logger.BeginMessageScope(context, LogLevel.Information);
        Logger.RedeclaredQueueOfReturnedMessage(ConnectionName, queueName);
    }
}
