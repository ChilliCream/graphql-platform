using System.Diagnostics;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Diagnostic observer that emits OpenTelemetry traces and metrics for dispatch, receive, and consume operations.
/// </summary>
/// <remarks>
/// Creates <see cref="Activity"/> spans for each pipeline stage and records exceptions as span events
/// with an error status. Trace context propagation is handled via message headers on the receive path,
/// enabling distributed tracing across transport boundaries.
/// </remarks>
public sealed class OpenTelemetryDiagnosticObserver : IBusDiagnosticObserver
{
    /// <inheritdoc />
    public IDisposable Dispatch(IDispatchContext context)
    {
        return DispatchActivity.Create(context);
    }

    /// <inheritdoc />
    public IDisposable Receive(IReceiveContext context)
    {
        return ReceiveActivity.Create(context);
    }

    /// <inheritdoc />
    public IDisposable Consume(IConsumeContext context)
    {
        return ConsumerActivity.Create(context);
    }

    /// <inheritdoc />
    public void OnReceiveError(IReceiveContext context, Exception exception)
    {
        Activity.Current?.AddException(exception);
        Activity.Current?.SetStatus(ActivityStatusCode.Error);
    }

    /// <inheritdoc />
    public void OnDispatchError(IDispatchContext context, Exception exception)
    {
        Activity.Current?.AddException(exception);
        Activity.Current?.SetStatus(ActivityStatusCode.Error);
    }

    /// <inheritdoc />
    public void OnConsumeError(IConsumeContext context, Exception exception)
    {
        Activity.Current?.AddException(exception);
        Activity.Current?.SetStatus(ActivityStatusCode.Error);
    }

    private sealed class ReceiveActivity : IDisposable
    {
        private readonly Activity? _activity;
        private readonly IReceiveContext _context;

        private ReceiveActivity(IReceiveContext context)
        {
            _context = context;

            var traceId = context.Headers.Get(MessageHeaders.TraceId);
            var traceState = context.Headers.Get(MessageHeaders.TraceState);
            var spanId = context.Headers.Get(MessageHeaders.SpanId);

            Activity? activity = null;

            if (!string.IsNullOrEmpty(traceId) && !string.IsNullOrEmpty(spanId))
            {
                var parentContext = new ActivityContext(
                    ActivityTraceId.CreateFromString(traceId),
                    ActivitySpanId.CreateFromString(spanId),
                    ActivityTraceFlags.Recorded,
                    traceState);

                activity = OpenTelemetry.Source.CreateActivity(
                    $"receive {context.Endpoint.Address}",
                    ActivityKind.Client,
                    parentContext);

                activity?.Start();
            }

            activity ??= OpenTelemetry.Source.StartActivity($"receive {context.Endpoint.Address}", ActivityKind.Client);
            _activity = activity;
        }

        public void Dispose()
        {
            // Enrich activity with context state after all middlewares have run
            if (_activity is not null)
            {
                _activity
                    .EnrichMessageDefault()
                    .SetMessageId(_context.MessageId ?? string.Empty)
                    .SetConversationId(_context.CorrelationId ?? string.Empty);
            }

            _activity?.Dispose();
        }

        public static ReceiveActivity Create(IReceiveContext context) => new(context);
    }

    private sealed class ConsumerActivity : IDisposable
    {
        private readonly Activity? _activity;
        private readonly IConsumeContext _context;

        private ConsumerActivity(IConsumeContext context)
        {
            _context = context;

            // TODO this can be done better!
            var currentConsumer = context.Features.Get<ReceiveConsumerFeature>()?.CurrentConsumer?.Name ?? "unknown";

            _activity = OpenTelemetry.Source.StartActivity($"consumer {currentConsumer}", ActivityKind.Consumer);
        }

        public void Dispose()
        {
            // Enrich activity with context state after all middlewares have run
            if (_activity is not null)
            {
                var consumerName = _context.MessageType is not null ? _context.MessageType.Identity : "unknown";

                _activity
                    .EnrichMessageDefault()
                    .SetMessageId(_context.MessageId ?? string.Empty)
                    .SetConversationId(_context.CorrelationId ?? string.Empty)
                    .SetConsumerName(consumerName);
            }

            _activity?.Dispose();
        }

        public static ConsumerActivity Create(IConsumeContext context) => new(context);
    }

    private sealed class DispatchActivity : IDisposable
    {
        private readonly Activity? _activity;
        private readonly long _startTime;
        private readonly string _operationType;
        private readonly IDispatchContext _context;

        private DispatchActivity(IDispatchContext context)
        {
            _operationType = "publish"; // TODO we need to get this from the route i guess
            _startTime = Stopwatch.GetTimestamp();
            _context = context;

            // Start activity early but don't enrich with state yet
            // State will be captured in Dispose() after all middlewares have run
            var operationName = $"{_operationType} {context.DestinationAddress}";
            _activity = OpenTelemetry
                .Source.StartActivity(operationName, ActivityKind.Producer)
                ?.SetOperationName(operationName)
                .SetOperationType(MessagingOperationType.Send)
                .EnrichMessageDefault();
        }

        public void Dispose()
        {
            var destination = _context.DestinationAddress;
            var operationName = $"{_operationType} {destination}";
            var transportName = _context.Transport.Name;

            // Enrich activity with context state after all middlewares have run
            if (_activity is not null)
            {
                _activity
                    .SetMessageId(_context.MessageId ?? string.Empty)
                    .SetConversationId(_context.ConversationId ?? string.Empty)
                    .SetInstanceId(_context.Host.InstanceId)
                    .SetDestinationTemporary(false)
                    .SetDestinationAddress(destination);
            }

            _activity?.Dispose();

            var elapsed = Stopwatch.GetElapsedTime(_startTime);

            OpenTelemetry.Meters.RecordOperationDuration(
                elapsed,
                operationName,
                destination,
                MessagingOperationType.Send,
                _context.Envelope?.MessageType ?? _context.MessageType?.Identity ?? string.Empty,
                transportName);

            OpenTelemetry.Meters.RecordSendMessage(
                operationName,
                destination,
                _context.Envelope?.MessageType ?? _context.MessageType?.Identity ?? string.Empty,
                transportName);
        }

        public static DispatchActivity Create(IDispatchContext context) => new(context);
    }
}
