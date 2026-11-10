using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using Mocha.Transport.RabbitMQ.Features;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

public sealed partial class RabbitMQDispatcher
{
    private static readonly TimeSpan s_publishWarningInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_slowPublishThreshold = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Moves a publish into the given phase and returns the phase's operation, which logs a warning
    /// every 30 seconds while it is pending.
    /// </summary>
    internal ValueTask RunPublishPhaseAsync(
        IDispatchContext context,
        RabbitMQPublishFeature publish,
        PublishPhase phase,
        ValueTask operation)
    {
        EnterPublishPhase(context, publish, phase);
        return operation.IsCompletedSuccessfully || !Logger.IsEnabled(LogLevel.Warning)
            ? operation
            : new ValueTask(WarnWhilePendingAsync(operation.AsTask(), context, publish.Channel, phase));
    }

    /// <inheritdoc cref="RunPublishPhaseAsync(IDispatchContext, RabbitMQPublishFeature, PublishPhase, ValueTask)" />
    internal async ValueTask<IChannel> RunPublishPhaseAsync(
        IDispatchContext context,
        RabbitMQPublishFeature publish,
        PublishPhase phase,
        ValueTask<IChannel> operation)
    {
        EnterPublishPhase(context, publish, phase);
        if (operation.IsCompletedSuccessfully || !Logger.IsEnabled(LogLevel.Warning))
        {
            return await operation;
        }

        var pending = operation.AsTask();
        await WarnWhilePendingAsync(pending, context, publish.Channel, phase);
        return await pending;
    }

    private void EnterPublishPhase(IDispatchContext context, RabbitMQPublishFeature publish, PublishPhase phase)
    {
        publish.Phase = phase;
        if (!Logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        using (BeginLogScope(publish.Channel))
        using (Logger.BeginMessageScope(context))
        {
            Logger.PublishWaiting(context.Envelope?.MessageId, phase);
        }
    }

    private async Task WarnWhilePendingAsync(
        Task operation,
        IDispatchContext context,
        IChannel? channel,
        PublishPhase phase)
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

                using (BeginLogScope(channel))
                using (Logger.BeginMessageScope(context))
                {
                    Logger.PublishStalled(context.Envelope?.MessageId, phase, TimeProvider.GetElapsedTime(started));
                }
            }
        }

        await operation;
    }

    internal void LogPublishRetry(
        IDispatchContext context,
        RabbitMQPublishFeature publish,
        Exception exception,
        TimeSpan delay)
    {
        if (!Logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        using (BeginLogScope(publish.Channel))
        using (Logger.BeginMessageScope(context))
        {
            Logger.PublishFailedRetrying(exception, ConnectionName, context.Endpoint.Name, publish.Retries, delay);
        }
    }

    internal void LogPublishOutcome(
        IDispatchContext context,
        RabbitMQPublishFeature publish,
        PublishOutcome outcome,
        Exception? exception)
    {
        var elapsed = Stopwatch.GetElapsedTime(publish.Started);
        var level = outcome == PublishOutcome.Failed ? LogLevel.Error
            : elapsed >= s_slowPublishThreshold ? LogLevel.Warning
            : LogLevel.Debug;
        if (!Logger.IsEnabled(level))
        {
            return;
        }

        using (BeginLogScope(publish.Channel))
        using (Logger.BeginMessageScope(context))
        {
            Logger.PublishFinished(
                exception,
                level,
                context.Envelope?.MessageId,
                outcome,
                publish.Phase,
                publish.Retries,
                elapsed);
        }
    }

    internal void LogReturnedQueueRepairStarted(IDispatchContext context, IChannel channel, string queueName)
    {
        using (BeginLogScope(channel))
        using (Logger.BeginMessageScope(context))
        {
            Logger.ReturnedQueueRepairStarted(ConnectionName, queueName);
        }
    }

    internal void LogReturnedQueueRedeclared(IDispatchContext context, IChannel channel, string queueName)
    {
        using (BeginLogScope(channel))
        using (Logger.BeginMessageScope(context))
        {
            Logger.RedeclaredQueueOfReturnedMessage(ConnectionName, queueName);
        }
    }
}
