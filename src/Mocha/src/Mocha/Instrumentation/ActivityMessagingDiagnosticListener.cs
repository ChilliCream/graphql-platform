using System.Collections.Immutable;
using System.Diagnostics;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha;

internal sealed class ActivityMessagingDiagnosticListener : MessagingDiagnosticEventListener
{
    public override IDisposable Dispatch(IDispatchContext context)
    {
        return DispatchActivity.Create(context);
    }

    public override void DispatchError(IDispatchContext context, Exception exception)
    {
        Activity.Current?.AddException(exception);
        Activity.Current?.SetStatus(ActivityStatusCode.Error);
    }

    public override IDisposable Receive(IReceiveContext context)
    {
        return ReceiveActivity.Create(context);
    }

    public override void ReceiveError(IReceiveContext context, Exception exception)
    {
        Activity.Current?.AddException(exception);
        Activity.Current?.SetStatus(ActivityStatusCode.Error);
    }

    public override IDisposable Consume(IConsumeContext context)
    {
        return ConsumerActivity.Create(context);
    }

    public override void ConsumeError(IConsumeContext context, Exception exception)
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

            var traceparent = context.Headers.Get(MessageHeaders.Traceparent);

            Activity? activity = null;

            if (!string.IsNullOrEmpty(traceparent))
            {
                var traceState = context.Headers.Get(MessageHeaders.Tracestate);
                if (ActivityContext.TryParse(traceparent, traceState, out var parentContext))
                {
                    activity = OpenTelemetry.Source.CreateActivity(
                        $"{context.Endpoint.Address} receive",
                        ActivityKind.Client,
                        parentContext);

                    activity?.Start();
                }
            }

            activity ??= OpenTelemetry.Source.StartActivity($"{context.Endpoint.Address} receive", ActivityKind.Client);
            _activity = activity;

            if (activity is not null)
            {
                context.Features.GetOrSet<ReceiveActivityFeature>().ActivityContext = activity.Context;
            }
        }

        public void Dispose()
        {
            // Enrich activity with context state after all middlewares have run
            _activity?
                .EnrichMessageDefault()
                .SetMessageId(_context.MessageId ?? string.Empty)
                .SetConversationId(_context.CorrelationId ?? string.Empty);

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

            _activity = context is IBatchConsumeContext batchContext
                ? OpenTelemetry.Source.StartActivity(
                    $"{currentConsumer} consume",
                    ActivityKind.Consumer,
                    default(ActivityContext),
                    null,
                    CreateLinks(batchContext))
                : OpenTelemetry.Source.StartActivity($"{currentConsumer} consume", ActivityKind.Consumer);
        }

        public void Dispose()
        {
            // Enrich activity with context state after all middlewares have run
            if (_activity is not null)
            {
                var batchConsumeContext = _context as IBatchConsumeContext;
                var consumerName = batchConsumeContext?.ItemMessageType?.Identity
                    ?? _context.MessageType?.Identity
                    ?? "unknown";

                _activity
                    .EnrichMessageDefault()
                    .SetMessageId(_context.MessageId)
                    .SetConversationId(_context.ConversationId)
                    .SetConsumerName(consumerName);

                if (batchConsumeContext is not null)
                {
                    _activity.SetTag("messaging.batch.id", batchConsumeContext.BatchId);
                    _activity.SetTag("messaging.batch.message_count", batchConsumeContext.Message.Count);
                }
            }

            _activity?.Dispose();
        }

        public static ConsumerActivity Create(IConsumeContext context) => new(context);

        private static ImmutableArray<ActivityLink> CreateLinks(IBatchConsumeContext batchContext)
        {
            var links = ImmutableArray.CreateBuilder<ActivityLink>(batchContext.Message.Count);
            var batch = batchContext.Message;

            for (var i = 0; i < batch.Count; i++)
            {
                if (batch.GetContext(i).Features.TryGet<ReceiveActivityFeature>(out var linkContext)
                    && linkContext.ActivityContext is { } activityContext)
                {
                    links.Add(new ActivityLink(activityContext));
                }
            }

            return links.ToImmutable();
        }
    }

    private sealed class ReceiveActivityFeature : IPooledFeature
    {
        public ActivityContext? ActivityContext { get; set; }

        public void Initialize(object state)
        {
            ActivityContext = null;
        }

        public void Reset()
        {
            ActivityContext = null;
        }
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
            var operationName = $"{context.DestinationAddress} {_operationType}";
            _activity = OpenTelemetry
                .Source.StartActivity(operationName, ActivityKind.Producer)
                ?.SetOperationName(operationName)
                .SetOperationType(MessagingOperationType.Send)
                .EnrichMessageDefault();
        }

        public void Dispose()
        {
            var destination = _context.DestinationAddress;
            var operationName = $"{destination} {_operationType}";
            var transportName = _context.Transport.Name;

            // Enrich activity with context state after all middlewares have run
            _activity?
                .SetMessageId(_context.MessageId ?? string.Empty)
                .SetConversationId(_context.ConversationId ?? string.Empty)
                .SetInstanceId(_context.Host.InstanceId)
                .SetDestinationTemporary(false)
                .SetDestinationAddress(destination);

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
