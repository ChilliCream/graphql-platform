using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal sealed partial class RabbitMQDispatcher
{
    private static readonly TimeSpan s_slowPublishThreshold = TimeSpan.FromSeconds(10);

    private readonly Stack<PublishDeadline> _deadlines = new();
    private ITimer? _stalledPublishCheck;
    private TimeSpan _stalledPublishInterval = TimeSpan.FromSeconds(30);
    private long _publishesStarted;
    private long _publishesFinished;
    private long _startedAtLastCheck;
    private long _finishedAtLastCheck;

    /// <summary>
    /// Gets or sets how often the dispatcher checks for publishes that wait for the broker. A publish that
    /// waits for a whole interval while no other publish finishes is reported.
    /// </summary>
    public TimeSpan StalledPublishInterval
    {
        get => _stalledPublishInterval;
        set
        {
            lock (_sync)
            {
                _stalledPublishInterval = value;
                _stalledPublishCheck?.Change(value, value);
            }
        }
    }

    /// <summary>
    /// Counts a publish as in flight and returns the deadline its operations observe, or <c>null</c> when
    /// the publish timeout is infinite and the operations observe the caller's token.
    /// </summary>
    public PublishDeadline? StartPublish(CancellationToken callerToken)
    {
        Interlocked.Increment(ref _publishesStarted);

        var timeout = PublishTimeout;
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            return null;
        }

        PublishDeadline? deadline;
        lock (_deadlines)
        {
            _deadlines.TryPop(out deadline);
        }

        deadline ??= new PublishDeadline();
        deadline.Start(timeout, callerToken);
        return deadline;
    }

    /// <summary>
    /// Counts a publish as finished and takes its deadline back.
    /// </summary>
    public void FinishPublish(PublishDeadline? deadline)
    {
        Interlocked.Increment(ref _publishesFinished);

        if (deadline is null)
        {
            return;
        }

        if (!deadline.TryReset())
        {
            deadline.Dispose();
            return;
        }

        lock (_deadlines)
        {
            if (_deadlines.Count < MaxPublisherChannels)
            {
                _deadlines.Push(deadline);
                return;
            }
        }

        deadline.Dispose();
    }

    /// <summary>
    /// Gets whether a failed publish is attempted again: the failure is transient, the publish was not
    /// cancelled, and the dispatcher is not disposed.
    /// </summary>
    public bool CanRetry(Exception exception, CancellationToken cancellationToken)
        => !cancellationToken.IsCancellationRequested
            && !IsDisposed
            && RabbitMQFailure.Classify(exception) == RabbitMQFailureKind.Transient;

    private void StartStalledPublishCheck()
    {
        lock (_sync)
        {
            if (_isDisposed || _stalledPublishCheck is not null)
            {
                return;
            }

            var interval = _stalledPublishInterval;
            _stalledPublishCheck = _connection.TimeProvider.CreateTimer(
                static state => ((RabbitMQDispatcher)state!).CheckForStalledPublishes(),
                this,
                interval,
                interval);
        }
    }

    private void StopStalledPublishCheck()
    {
        ITimer? timer;
        lock (_sync)
        {
            timer = _stalledPublishCheck;
            _stalledPublishCheck = null;
        }

        timer?.Dispose();
    }

    private void CheckForStalledPublishes()
    {
        // Read in this order, the number in flight is never negative.
        var finished = Volatile.Read(ref _publishesFinished);
        var started = Volatile.Read(ref _publishesStarted);
        var stalled = _startedAtLastCheck - _finishedAtLastCheck;

        // No publish finished since the last check, so every publish that was in flight then still is.
        if (stalled > 0 && finished == _finishedAtLastCheck && _logger.IsEnabled(LogLevel.Warning))
        {
            var status = !_connection.IsConnected ? "unavailable"
                : _connection.IsBlocked ? "blocked by the broker"
                : "ready";

            using (_connection.BeginLogScope())
            {
                _logger.PublishesStalled(_connection.ConnectionName, stalled, StalledPublishInterval, status);
            }
        }

        _startedAtLastCheck = started;
        _finishedAtLastCheck = finished;
    }

    public void LogPublishConfirmed(IDispatchContext context, int attempt, long started)
    {
        var elapsed = Stopwatch.GetElapsedTime(started);
        var isSlow = elapsed >= s_slowPublishThreshold;
        if (!_logger.IsEnabled(isSlow ? LogLevel.Warning : LogLevel.Debug))
        {
            return;
        }

        using (_connection.BeginLogScope())
        using (_logger.BeginMessageScope(context))
        {
            var messageId = context.Envelope?.MessageId;
            var endpoint = context.Endpoint.Name;
            if (isSlow)
            {
                _logger.PublishConfirmedSlowly(
                    messageId,
                    endpoint,
                    _connection.ConnectionName,
                    attempt,
                    elapsed,
                    s_slowPublishThreshold);
            }
            else
            {
                _logger.PublishConfirmed(messageId, endpoint, _connection.ConnectionName, attempt, elapsed);
            }
        }
    }

    public void LogPublishFailed(IDispatchContext context, Exception exception, int attempt, long started)
    {
        if (!_logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        using (_connection.BeginLogScope())
        using (_logger.BeginMessageScope(context))
        {
            _logger.PublishFailed(
                exception,
                context.Envelope?.MessageId,
                context.Endpoint.Name,
                _connection.ConnectionName,
                attempt,
                Stopwatch.GetElapsedTime(started));
        }
    }

    public void LogPublishCancelled(IDispatchContext context, int attempt, long started)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        using (_connection.BeginLogScope())
        using (_logger.BeginMessageScope(context))
        {
            _logger.PublishCancelled(
                context.Envelope?.MessageId,
                context.Endpoint.Name,
                _connection.ConnectionName,
                attempt,
                Stopwatch.GetElapsedTime(started));
        }
    }

    public void LogPublishRetry(IDispatchContext context, Exception exception, int attempt, TimeSpan delay)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        using (_connection.BeginLogScope())
        using (_logger.BeginMessageScope(context))
        {
            _logger.PublishRetryScheduled(
                context.Envelope?.MessageId,
                context.Endpoint.Name,
                _connection.ConnectionName,
                attempt,
                delay,
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    public void LogMessageReturned(IDispatchContext context, string queueName)
    {
        using (_connection.BeginLogScope())
        using (_logger.BeginMessageScope(context))
        {
            _logger.MessageReturned(context.Envelope?.MessageId, queueName, _connection.ConnectionName);
        }
    }

    public void LogQueueRedeclared(IDispatchContext context, string queueName)
    {
        using (_connection.BeginLogScope())
        using (_logger.BeginMessageScope(context))
        {
            _logger.QueueRedeclared(queueName, _connection.ConnectionName, context.Envelope?.MessageId);
        }
    }

    private void LogPublisherChannelOpened(IChannel channel)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        using (_connection.BeginLogScope())
        {
            _logger.PublisherChannelOpened(_connection.ConnectionName, channel.ChannelNumber);
        }
    }
}
