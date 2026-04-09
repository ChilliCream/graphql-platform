using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Testing.Internal;

namespace Mocha.Testing;

/// <summary>
/// Diagnostic event listener that tracks all message lifecycle events and provides
/// completion detection for integration tests.
/// </summary>
/// <remarks>
/// <para>
/// Create an instance externally and register it in one or more independent bus hosts
/// via <see cref="MessageTrackingExtensions.AddMessageTracking(IServiceCollection, MessageTracker)"/>
/// for cross-host tracking, or attach to an already-running host via <see cref="Attach"/>.
/// </para>
/// </remarks>
public sealed class MessageTracker : MessagingDiagnosticEventListener, IMessageTracker
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DebuggerTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan DispatchedOnlyGracePeriod = TimeSpan.FromMilliseconds(100);

    private IServiceProvider? _serviceProvider;
    private readonly ConcurrentDictionary<string, EnvelopeTracker> _envelopes = new();
    private readonly ConcurrentQueue<TrackedMessage> _dispatched = new();
    private readonly ConcurrentQueue<TrackedMessage> _consumed = new();
    private readonly ConcurrentQueue<TrackedMessage> _failed = new();
    private readonly ConcurrentQueue<TrackedEvent> _timeline = new();
    private readonly StubRegistry _stubs = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageTracker"/> class.
    /// </summary>
    public MessageTracker()
    {
    }

    /// <summary>
    /// Attaches this tracker to an already-running bus host, subscribing to its
    /// diagnostic events at runtime. Returns an <see cref="IDisposable"/> that
    /// detaches the tracker when disposed.
    /// </summary>
    /// <param name="host">The host's service provider (application-level).</param>
    /// <returns>A subscription that detaches the tracker when disposed.</returns>
    public IDisposable Attach(IServiceProvider host)
    {
        var runtime = (MessagingRuntime)host.GetRequiredService<IMessagingRuntime>();
        var aggregate = runtime.Services.GetRequiredService<AggregateMessagingDiagnosticEvents>();
        return aggregate.Subscribe(this);
    }

    /// <summary>
    /// Sets the service provider used to resolve dependencies for stub execution.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    internal void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // High-water marks for delta tracking (guarded by _watermarkLock)
    private readonly object _watermarkLock = new();
    private int _dispatchedWatermark;
    private int _consumedWatermark;
    private int _failedWatermark;

    // Completion signaling
    private readonly object _completionLock = new();
    private TaskCompletionSource<bool>? _completionTcs;

    // WaitForConsumed registrations — bag per type to support concurrent waiters
    private readonly ConcurrentDictionary<Type, ConcurrentBag<IConsumedWaiter>> _consumedWaiters = new();

    /// <inheritdoc />
    public IReadOnlyList<TrackedMessage> Dispatched => _dispatched.ToArray();

    /// <inheritdoc />
    public IReadOnlyList<TrackedMessage> Consumed => _consumed.ToArray();

    /// <inheritdoc />
    public IReadOnlyList<TrackedMessage> Failed => _failed.ToArray();

    /// <inheritdoc />
    public IReadOnlyList<TrackedEvent> Timeline => _timeline.ToArray();

    /// <inheritdoc />
    public override IDisposable Dispatch(IDispatchContext context)
    {
        var messageId = context.MessageId;
        var message = context.Message;
        var messageType = context.MessageType?.RuntimeType ?? message?.GetType() ?? typeof(object);
        var destinationAddress = context.DestinationAddress?.ToString();
        var correlationId = context.CorrelationId;
        var dispatchKind = DetermineDispatchKind(context);
        var timestamp = _stopwatch.Elapsed;

        var tracked = new TrackedMessage
        {
            Message = message!,
            MessageType = messageType,
            DispatchKind = dispatchKind,
            Timestamp = timestamp,
            MessageId = messageId,
            CorrelationId = correlationId
        };

        _dispatched.Enqueue(tracked);

        _timeline.Enqueue(new TrackedEvent
        {
            Kind = TrackedEventKind.Dispatched,
            MessageType = messageType,
            Timestamp = timestamp,
            MessageId = messageId
        });

        // Create or get envelope tracker
        var key = BuildEnvelopeKey(messageId, destinationAddress);
        EnvelopeTracker? envelopeTracker = null;
        if (key is not null)
        {
            envelopeTracker = _envelopes.GetOrAdd(key, static k => new EnvelopeTracker(k));
            envelopeTracker.Record(TrackedEventKind.Dispatched);
        }

        // If this is a sent message and a stub is registered, invoke the stub
        // and publish the response asynchronously.
        if (dispatchKind == MessageDispatchKind.Sent
            && message is not null
            && _serviceProvider is not null
            && _stubs.TryGetStub(messageType, out var factory))
        {
            var capturedEnvelopeTracker = envelopeTracker;
            var capturedServiceProvider = _serviceProvider;
            _ = Task.Run(async () =>
            {
                try
                {
                    var response = factory!(message);
                    using var scope = capturedServiceProvider.CreateScope();
                    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                    await bus.PublishAsync(response, CancellationToken.None);
                }
                finally
                {
                    // Mark the envelope as complete since the stub acted as the consumer.
                    if (capturedEnvelopeTracker is not null)
                    {
                        capturedEnvelopeTracker.Record(TrackedEventKind.ConsumeCompleted);
                        SignalCompletionIfReady();
                    }
                }
            });
        }

        return EmptyScope;
    }

    /// <inheritdoc />
    public override void DispatchError(IDispatchContext context, Exception exception)
    {
        var messageType = context.MessageType?.RuntimeType ?? context.Message?.GetType() ?? typeof(object);

        _timeline.Enqueue(new TrackedEvent
        {
            Kind = TrackedEventKind.Dispatched,
            MessageType = messageType,
            Timestamp = _stopwatch.Elapsed,
            MessageId = context.MessageId,
            Exception = exception
        });
    }

    /// <inheritdoc />
    public override IDisposable Receive(IReceiveContext context)
    {
        var messageId = context.MessageId;
        var destinationAddress = context.DestinationAddress?.ToString();
        var messageType = context.MessageType?.RuntimeType ?? typeof(object);
        var timestamp = _stopwatch.Elapsed;

        _timeline.Enqueue(new TrackedEvent
        {
            Kind = TrackedEventKind.Received,
            MessageType = messageType,
            Timestamp = timestamp,
            MessageId = messageId
        });

        // Create envelope tracker for receive (handles fan-out: same MessageId, different DestinationAddress)
        var key = BuildEnvelopeKey(messageId, destinationAddress);
        if (key is not null)
        {
            var tracker = _envelopes.GetOrAdd(key, static k => new EnvelopeTracker(k));
            tracker.Record(TrackedEventKind.Received);
        }

        return EmptyScope;
    }

    /// <inheritdoc />
    public override void ReceiveError(IReceiveContext context, Exception exception)
    {
        var messageType = context.MessageType?.RuntimeType ?? typeof(object);

        _timeline.Enqueue(new TrackedEvent
        {
            Kind = TrackedEventKind.Received,
            MessageType = messageType,
            Timestamp = _stopwatch.Elapsed,
            MessageId = context.MessageId,
            Exception = exception
        });
    }

    /// <inheritdoc />
    public override IDisposable Consume(IConsumeContext context)
    {
        var messageId = context.MessageId;
        var destinationAddress = context.DestinationAddress?.ToString();
        var messageType = context.MessageType?.RuntimeType ?? typeof(object);
        var timestamp = _stopwatch.Elapsed;

        _timeline.Enqueue(new TrackedEvent
        {
            Kind = TrackedEventKind.ConsumeStarted,
            MessageType = messageType,
            Timestamp = timestamp,
            MessageId = messageId
        });

        return new ConsumeScope(this, context, messageId, destinationAddress, messageType, timestamp);
    }

    /// <inheritdoc />
    public override void ConsumeError(IConsumeContext context, Exception exception)
    {
        var messageId = context.MessageId;
        var destinationAddress = context.DestinationAddress?.ToString();
        var messageType = context.MessageType?.RuntimeType ?? typeof(object);
        var timestamp = _stopwatch.Elapsed;

        // Try to get the message for tracking
        object? message = null;
        try
        {
            message = context.GetMessage();
        }
        catch
        {
            // Message may not be deserializable at this point
        }

        _failed.Enqueue(new TrackedMessage
        {
            Message = message ?? new object(),
            MessageType = messageType,
            DispatchKind = LookupDispatchKind(messageId),
            Timestamp = timestamp,
            MessageId = messageId,
            Exception = exception
        });

        _timeline.Enqueue(new TrackedEvent
        {
            Kind = TrackedEventKind.ConsumeFailed,
            MessageType = messageType,
            Timestamp = timestamp,
            MessageId = messageId,
            Exception = exception
        });

        // Mark envelope as complete
        var key = BuildEnvelopeKey(messageId, destinationAddress);
        if (key is not null && _envelopes.TryGetValue(key, out var tracker))
        {
            tracker.Record(TrackedEventKind.ConsumeFailed);
            SignalCompletionIfReady();
        }
    }

    /// <inheritdoc />
    public async Task<MessageTrackingResult> WaitForCompletionAsync(
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var effectiveTimeout = timeout ?? GetDefaultTimeout();
        var startTime = _stopwatch.Elapsed;

        // Snapshot current watermarks under lock to avoid race with concurrent enqueues
        int dispatchStart, consumeStart, failStart;
        lock (_watermarkLock)
        {
            dispatchStart = _dispatchedWatermark;
            _dispatchedWatermark = _dispatched.Count;
            consumeStart = _consumedWatermark;
            _consumedWatermark = _consumed.Count;
            failStart = _failedWatermark;
            _failedWatermark = _failed.Count;
        }

        // Wait for all envelopes to complete
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(effectiveTimeout);

        try
        {
            await WaitForAllEnvelopesAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timeout — build diagnostic output and throw
            var pendingKeys = GetPendingEnvelopeKeys();
            var diagnosticOutput = DiagnosticFormatter.FormatTimeout(
                effectiveTimeout,
                Timeline,
                pendingKeys);

            throw new MessageTrackingException(
                $"Message tracking timed out after {effectiveTimeout.TotalSeconds:F1}s.",
                diagnosticOutput);
        }

        // Snapshot end watermarks under lock to capture cascading work atomically
        int dispatchEnd, consumeEnd, failEnd;
        lock (_watermarkLock)
        {
            dispatchEnd = _dispatched.Count;
            consumeEnd = _consumed.Count;
            failEnd = _failed.Count;
            _dispatchedWatermark = dispatchEnd;
            _consumedWatermark = consumeEnd;
            _failedWatermark = failEnd;
        }

        var elapsed = _stopwatch.Elapsed - startTime;
        var allDispatched = _dispatched.ToArray();
        var allConsumed = _consumed.ToArray();
        var allFailed = _failed.ToArray();

        return new MessageTrackingResult(
            GetSlice(allDispatched, dispatchStart, dispatchEnd),
            GetSlice(allConsumed, consumeStart, consumeEnd),
            GetSlice(allFailed, failStart, failEnd),
            completed: true,
            elapsed);
    }

    /// <inheritdoc />
    public async Task<T> WaitForConsumed<T>(
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var effectiveTimeout = timeout ?? GetDefaultTimeout();
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiter = new ConsumedWaiter<T>(tcs);

        // Add to bag — multiple concurrent waiters for the same type are supported
        var bag = _consumedWaiters.GetOrAdd(typeof(T), static _ => new ConcurrentBag<IConsumedWaiter>());
        bag.Add(waiter);

        // Check if already consumed
        foreach (var msg in _consumed)
        {
            if (msg.Message is T existing)
            {
                tcs.TrySetResult(existing);
                break;
            }
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(effectiveTimeout);
        await using var registration = cts.Token.Register(() => tcs.TrySetCanceled(cts.Token));

        try
        {
            return await tcs.Task;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            var diagnosticOutput = DiagnosticFormatter.FormatTimeout(
                effectiveTimeout,
                Timeline,
                GetPendingEnvelopeKeys());

            throw new MessageTrackingException(
                $"Timed out waiting for {typeof(T).Name} to be consumed.",
                diagnosticOutput);
        }
    }

    /// <inheritdoc />
    public string ToDiagnosticString()
    {
        return DiagnosticFormatter.FormatTimeline(Timeline);
    }

    /// <inheritdoc />
    public IMessageStubBuilder<T> WhenSent<T>()
    {
        return new MessageStubBuilder<T>(_stubs);
    }

    private void OnConsumeCompleted(
        IConsumeContext context,
        string? messageId,
        string? destinationAddress,
        Type messageType,
        TimeSpan startTimestamp)
    {
        var endTimestamp = _stopwatch.Elapsed;
        var duration = endTimestamp - startTimestamp;

        // Check if this envelope already failed. ConsumeError is called before
        // Dispose, so if it already failed we should not also add it to consumed.
        var key = BuildEnvelopeKey(messageId, destinationAddress);
        EnvelopeTracker? envelopeTracker = null;
        if (key is not null)
        {
            _envelopes.TryGetValue(key, out envelopeTracker);
        }

        var alreadyFailed = envelopeTracker?.HasFailed() == true;

        if (!alreadyFailed)
        {
            // Try to get the message for tracking
            object? message = null;
            try
            {
                message = context.GetMessage();
            }
            catch
            {
                // Message may not be deserializable at this point
            }

            _consumed.Enqueue(new TrackedMessage
            {
                Message = message ?? new object(),
                MessageType = messageType,
                DispatchKind = LookupDispatchKind(messageId),
                Timestamp = endTimestamp,
                MessageId = messageId
            });

            // Signal WaitForConsumed<T> waiters
            if (message is not null)
            {
                NotifyConsumedWaiters(message);
            }
        }

        _timeline.Enqueue(new TrackedEvent
        {
            Kind = TrackedEventKind.ConsumeCompleted,
            MessageType = messageType,
            Timestamp = endTimestamp,
            MessageId = messageId,
            Duration = duration
        });

        // Mark envelope as complete
        if (envelopeTracker is not null)
        {
            envelopeTracker.Record(TrackedEventKind.ConsumeCompleted);
            SignalCompletionIfReady();
        }
    }

    private void NotifyConsumedWaiters(object message)
    {
        var messageType = message.GetType();

        foreach (var kvp in _consumedWaiters)
        {
            if (kvp.Key.IsAssignableFrom(messageType))
            {
                foreach (var waiter in kvp.Value)
                {
                    waiter.TrySignal(message);
                }
            }
        }
    }

    private async Task WaitForAllEnvelopesAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (AreAllEnvelopesComplete())
            {
                return;
            }

            // Check if only dispatched-only envelopes remain (no subscriber).
            // Give a short grace period for late Receive events, then treat as complete.
            if (AreOnlyDispatchedOnlyEnvelopesPending())
            {
                await Task.Delay(DispatchedOnlyGracePeriod, ct);
                if (AreAllEnvelopesComplete() || AreOnlyDispatchedOnlyEnvelopesPending())
                {
                    return;
                }

                // A Receive arrived during the grace period — continue waiting normally.
            }

            TaskCompletionSource<bool> tcs;
            lock (_completionLock)
            {
                if (AreAllEnvelopesComplete())
                {
                    return;
                }

                if (AreOnlyDispatchedOnlyEnvelopesPending())
                {
                    return;
                }

                _completionTcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                tcs = _completionTcs;
            }

            await using var registration = ct.Register(() => tcs.TrySetCanceled(ct));
            await tcs.Task;
        }

        ct.ThrowIfCancellationRequested();
    }

    private bool AreAllEnvelopesComplete()
    {
        if (_envelopes.IsEmpty)
        {
            return true;
        }

        foreach (var kvp in _envelopes)
        {
            if (!kvp.Value.IsComplete())
            {
                return false;
            }
        }

        return true;
    }

    private bool AreOnlyDispatchedOnlyEnvelopesPending()
    {
        if (_envelopes.IsEmpty)
        {
            return false;
        }

        foreach (var kvp in _envelopes)
        {
            if (!kvp.Value.IsComplete() && !kvp.Value.IsDispatchedOnly())
            {
                return false;
            }
        }

        return true;
    }

    private void SignalCompletionIfReady()
    {
        if (!AreAllEnvelopesComplete())
        {
            return;
        }

        lock (_completionLock)
        {
            if (_completionTcs is not null)
            {
                _completionTcs.TrySetResult(true);
                _completionTcs = null;
            }
        }
    }

    private IReadOnlyList<string> GetPendingEnvelopeKeys()
    {
        var pending = new List<string>();

        foreach (var kvp in _envelopes)
        {
            if (!kvp.Value.IsComplete())
            {
                pending.Add(kvp.Key);
            }
        }

        return pending;
    }

    private static MessageDispatchKind DetermineDispatchKind(IDispatchContext context)
    {
        var messageKind = context.Headers.GetMessageKind();

        return messageKind switch
        {
            Mocha.MessageKind.Publish => MessageDispatchKind.Published,
            Mocha.MessageKind.Send => MessageDispatchKind.Sent,
            Mocha.MessageKind.Request => MessageDispatchKind.Sent,
            _ => MessageDispatchKind.Published
        };
    }

    private static string? BuildEnvelopeKey(string? messageId, string? destinationAddress)
    {
        if (messageId is null)
        {
            return null;
        }

        return destinationAddress is not null
            ? $"{messageId}|{destinationAddress}"
            : messageId;
    }

    private MessageDispatchKind LookupDispatchKind(string? messageId)
    {
        if (messageId is null)
        {
            return MessageDispatchKind.Published;
        }

        // Look up from the dispatched queue — it already has the correct DispatchKind
        foreach (var msg in _dispatched)
        {
            if (msg.MessageId == messageId)
            {
                return msg.DispatchKind;
            }
        }

        return MessageDispatchKind.Published;
    }

    private static TimeSpan GetDefaultTimeout()
    {
        return Debugger.IsAttached ? DebuggerTimeout : DefaultTimeout;
    }

    private static IReadOnlyList<TrackedMessage> GetSlice(
        TrackedMessage[] source,
        int start,
        int end)
    {
        if (start >= end || start >= source.Length)
        {
            return [];
        }

        var length = Math.Min(end, source.Length) - start;
        var slice = new TrackedMessage[length];
        Array.Copy(source, start, slice, 0, length);
        return slice;
    }

    private interface IConsumedWaiter
    {
        bool TrySignal(object message);
    }

    private sealed class ConsumedWaiter<T> : IConsumedWaiter
    {
        private readonly TaskCompletionSource<T> _tcs;

        public ConsumedWaiter(TaskCompletionSource<T> tcs) => _tcs = tcs;

        public bool TrySignal(object message)
        {
            if (message is T typed)
            {
                return _tcs.TrySetResult(typed);
            }

            return false;
        }
    }

    private sealed class ConsumeScope : IDisposable
    {
        private readonly MessageTracker _tracker;
        private readonly IConsumeContext _context;
        private readonly string? _messageId;
        private readonly string? _destinationAddress;
        private readonly Type _messageType;
        private readonly TimeSpan _startTimestamp;

        public ConsumeScope(
            MessageTracker tracker,
            IConsumeContext context,
            string? messageId,
            string? destinationAddress,
            Type messageType,
            TimeSpan startTimestamp)
        {
            _tracker = tracker;
            _context = context;
            _messageId = messageId;
            _destinationAddress = destinationAddress;
            _messageType = messageType;
            _startTimestamp = startTimestamp;
        }

        public void Dispose()
        {
            _tracker.OnConsumeCompleted(_context, _messageId, _destinationAddress, _messageType, _startTimestamp);
        }
    }
}
