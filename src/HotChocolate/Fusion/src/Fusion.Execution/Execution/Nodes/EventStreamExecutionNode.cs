using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Subscriptions;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class EventStreamExecutionNode : ExecutionNode
{
    private static readonly JsonWriterOptions s_eventMessageWriterOptions = new()
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly EventStreamSource _eventStreamSource;
    private readonly string _message;
    private readonly ResultSelectionSet _resultSelectionSet;
    private readonly ExecutionNodeCondition[] _conditions;
    private readonly SelectionPath _source;
    private readonly SelectionPath _target;

    internal EventStreamExecutionNode(
        int id,
        string fieldName,
        SelectionPath target,
        SelectionPath source,
        ResultSelectionSet resultSelectionSet,
        EventStreamSource eventStreamSource,
        string message,
        ExecutionNodeCondition[] conditions)
    {
        ArgumentException.ThrowIfNullOrEmpty(fieldName);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(resultSelectionSet);
        ArgumentNullException.ThrowIfNull(eventStreamSource);
        ArgumentException.ThrowIfNullOrEmpty(message);
        ArgumentNullException.ThrowIfNull(conditions);

        Id = id;
        FieldName = fieldName;
        _target = target;
        _source = source;
        _resultSelectionSet = resultSelectionSet;
        _eventStreamSource = eventStreamSource;
        _message = message;
        _conditions = conditions;
    }

    public override int Id { get; }

    public override ExecutionNodeType Type => ExecutionNodeType.EventStream;

    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => _conditions;

    public override string? SchemaName => null;

    public string FieldName { get; }

    public SelectionPath Target => _target;

    public SelectionPath Source => _source;

    internal ResultSelectionSet ResultSelectionSet => _resultSelectionSet;

    internal EventStreamSource EventStreamSource => _eventStreamSource;

    internal string Message => _message;

    protected override ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "Event stream execution nodes are started by the subscription executor.");

    internal SubscriptionResult Subscribe(OperationPlanContext context)
    {
        var subscriptionId = SubscriptionId.Next();

        try
        {
            var stream = new EventStreamSubscriptionEnumerable(
                context,
                this,
                subscriptionId,
                context.DiagnosticEvents);

            return SubscriptionResult.Success(subscriptionId, stream);
        }
        catch (InvalidEventMessageCursorException ex)
        {
            context.AddErrors(ErrorBuilder.FromException(ex).Build(), _resultSelectionSet, Path.Root);
            context.DiagnosticEvents.ExecutionNodeError(context, this, ex);
            return SubscriptionResult.Failed(subscriptionId, ex);
        }
        catch (Exception ex)
        {
            context.AddErrors(ErrorBuilder.FromException(ex).Build(), _resultSelectionSet, Path.Root);
            context.DiagnosticEvents.ExecutionNodeError(context, this, ex);
            return SubscriptionResult.Failed(subscriptionId, ex);
        }
    }

    private sealed class EventStreamSubscriptionEnumerable(
        OperationPlanContext context,
        EventStreamExecutionNode node,
        ulong subscriptionId,
        IFusionExecutionDiagnosticEvents diagnosticEvents)
        : IAsyncEnumerable<EventMessageResult>
    {
        private readonly OperationPlanContext _context = context;
        private readonly EventStreamExecutionNode _node = node;
        private readonly ulong _subscriptionId = subscriptionId;
        private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents = diagnosticEvents;

        public IAsyncEnumerator<EventMessageResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            try
            {
                return new EventStreamSubscriptionEnumerator(
                    _context,
                    _node,
                    _subscriptionId,
                    _diagnosticEvents,
                    cancellationToken);
            }
            catch (InvalidEventMessageCursorException exception)
            {
                _context.AddErrors(
                    ErrorBuilder.FromException(exception).SetMessage(exception.Message).Build(),
                    _node._resultSelectionSet,
                    Path.Root);
                _diagnosticEvents.ExecutionNodeError(_context, _node, exception);
                return new FailedEventStreamSubscriptionEnumerator(_node.Id, exception);
            }
            catch (Exception exception)
            {
                _context.AddErrors(
                    ErrorBuilder.FromException(exception).Build(),
                    _node._resultSelectionSet,
                    Path.Root);
                _diagnosticEvents.ExecutionNodeError(_context, _node, exception);

                return new FailedEventStreamSubscriptionEnumerator(_node.Id, exception);
            }
        }
    }

    private sealed class FailedEventStreamSubscriptionEnumerator(int nodeId, Exception exception)
        : IAsyncEnumerator<EventMessageResult>
    {
        private bool _yielded;

        public EventMessageResult Current { get; private set; } = null!;

        public ValueTask<bool> MoveNextAsync()
        {
            if (_yielded)
            {
                Current = null!;
                return new ValueTask<bool>(false);
            }

            _yielded = true;
            var timestamp = Stopwatch.GetTimestamp();
            Current = new EventMessageResult(
                nodeId,
                Activity.Current,
                ExecutionStatus.Failed,
                Disposable.Empty,
                timestamp,
                Stopwatch.GetTimestamp(),
                exception,
                VariableValueSets: [],
                DependentsToExecute: []);

            return new ValueTask<bool>(true);
        }

        public ValueTask DisposeAsync() => default;
    }

    private sealed class EventStreamSubscriptionEnumerator : IAsyncEnumerator<EventMessageResult>
    {
        private readonly OperationPlanContext _context;
        private readonly EventStreamExecutionNode _node;
        private readonly ulong _subscriptionId;
        private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _disposeCts;
        private readonly IDisposable _subscriptionScope;
        private readonly BrokerSubscription _subscription;
        private readonly SourceSchemaResult[] _resultBuffer = new SourceSchemaResult[1];
        private bool _disposed;

        public EventStreamSubscriptionEnumerator(
            OperationPlanContext context,
            EventStreamExecutionNode node,
            ulong subscriptionId,
            IFusionExecutionDiagnosticEvents diagnosticEvents,
            CancellationToken cancellationToken)
        {
            _context = context;
            _node = node;
            _subscriptionId = subscriptionId;
            _diagnosticEvents = diagnosticEvents;
            _cancellationToken = cancellationToken;
            _disposeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _subscriptionScope = diagnosticEvents.ExecuteSubscription(context.RequestContext, _subscriptionId);

            var source = node._eventStreamSource;
            var subscriptionContext = new SubscriptionFieldContext(
                context,
                node.FieldName,
                requiresCursor: !string.IsNullOrEmpty(source.CursorField));
            var cursor = ResolveCursor(source.CursorArgument, subscriptionContext);
            var broker = context.RequestContext.RequestServices
                .GetRequiredService<IEventStreamBrokerFactory>()
                .Create(source.Broker);
            var topics = ResolveTopics(source.Topics, context, node);
            var enumerator = broker
                .SubscribeAsync(subscriptionContext, topics, cursor, _disposeCts.Token)
                .GetAsyncEnumerator(_disposeCts.Token);

            _subscription = new BrokerSubscription(source, broker, enumerator);
        }

        public EventMessageResult Current { get; private set; } = null!;

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_disposed || _cancellationToken.IsCancellationRequested)
            {
                Current = null!;
                return false;
            }

            if (!await _subscription.Enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                Current = null!;
                return false;
            }

            Current = ProcessEvent(_subscription, _subscription.Enumerator.Current);
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await _disposeCts.CancelAsync().ConfigureAwait(false);
            await _subscription.DisposeAsync().ConfigureAwait(false);
            _subscriptionScope.Dispose();
            _disposeCts.Dispose();
        }

        private EventMessageResult ProcessEvent(BrokerSubscription subscription, EventMessage message)
        {
            SourceSchemaResult? sourceResult = null;
            IDisposable? scope = null;
            var arenaBefore = subscription.ArenaSource.Arena;
            var start = Stopwatch.GetTimestamp();

            try
            {
                scope = _diagnosticEvents.ExecuteSubscriptionNode(
                    _context,
                    _node,
                    subscription.Source.SchemaName,
                    _subscriptionId);

                using (message)
                {
                    var arena = subscription.ArenaSource.GetNextArena();
                    var document = DecodeMessage(
                        arena,
                        message,
                        _node.FieldName,
                        _node._eventStreamSource.CursorField);
                    sourceResult = new SourceSchemaResult(CompactPath.Root, document);
                    _resultBuffer[0] = sourceResult;

                    _context.SetActiveEventArena(subscription.ArenaSource.Arena);
                    _context.AddPartialResults(
                        _node._source,
                        _resultBuffer,
                        _node._resultSelectionSet,
                        containsErrors: true);

                    sourceResult = null;
                }

                return new EventMessageResult(
                    _node.Id,
                    Activity.Current,
                    ExecutionStatus.Success,
                    scope,
                    start,
                    Stopwatch.GetTimestamp(),
                    Exception: null,
                    VariableValueSets: [],
                    DependentsToExecute: default);
            }
            catch (Exception exception)
            {
                message.Dispose();
                sourceResult?.Dispose();

                if (!ReferenceEquals(subscription.ArenaSource.Arena, arenaBefore))
                {
                    ((IDisposable)subscription.ArenaSource.Arena).Dispose();
                }

                scope ??= Disposable.Empty;

                _context.DiagnosticEvents.SubscriptionEventError(
                    _context,
                    _node,
                    subscription.Source.SchemaName,
                    _subscriptionId,
                    exception);
                _context.AddErrors(
                    ErrorBuilder.FromException(exception).Build(),
                    _node._resultSelectionSet,
                    Path.Root);

                return new EventMessageResult(
                    _node.Id,
                    Activity.Current,
                    ExecutionStatus.Failed,
                    scope,
                    start,
                    Stopwatch.GetTimestamp(),
                    Exception: exception,
                    VariableValueSets: [],
                    DependentsToExecute: default);
            }
        }

        private static string[] ResolveTopics(
            ImmutableArray<string> topics,
            OperationPlanContext context,
            EventStreamExecutionNode node)
        {
            if (topics.IsDefaultOrEmpty)
            {
                return [node.FieldName];
            }

            var resolvedTopics = new string[topics.Length];

            for (var i = 0; i < topics.Length; i++)
            {
                resolvedTopics[i] = ResolveTopic(topics[i], context, node);
            }

            return resolvedTopics;
        }

        private static string ResolveTopic(
            string topic,
            OperationPlanContext context,
            EventStreamExecutionNode node)
            => TopicTemplate.Expand(
                topic,
                expression => ResolveTopicExpression(expression, context, node));

        private static string ResolveTopicExpression(
            ReadOnlySpan<char> expression,
            OperationPlanContext context,
            EventStreamExecutionNode node)
        {
            if (expression.SequenceEqual("context.path"))
            {
                return ResolveContextPath(node);
            }

            const string argsPrefix = "args.";

            if (expression.StartsWith(argsPrefix, StringComparison.Ordinal))
            {
                return ResolveArgument(
                    expression[argsPrefix.Length..].ToString(),
                    context,
                    node);
            }

            throw new InvalidOperationException(
                $"The event stream topic expression '{{${expression.ToString()}}}' is not supported.");
        }

        private static string ResolveArgument(
            string argumentName,
            OperationPlanContext context,
            EventStreamExecutionNode node)
        {
            var rootField = GetRootField(context.OperationPlan.Operation.Definition, node.FieldName);

            foreach (var argument in rootField.Arguments)
            {
                if (argument.Name.Value.Equals(argumentName, StringComparison.Ordinal))
                {
                    var value = argument.Value is VariableNode variable
                        ? context.Variables.GetValue<IValueNode>(variable.Name.Value)
                        : argument.Value;

                    return FormatTopicValue(value);
                }
            }

            throw new InvalidOperationException(
                $"The event stream topic references an unknown argument '{argumentName}'.");
        }

        private static FieldNode GetRootField(
            OperationDefinitionNode definition,
            string responseName)
        {
            foreach (var selection in definition.SelectionSet.Selections)
            {
                if (selection is FieldNode field
                    && !field.Name.Value.Equals(IntrospectionFieldNames.TypeName, StringComparison.Ordinal)
                    && (field.Alias?.Value ?? field.Name.Value).Equals(responseName, StringComparison.Ordinal))
                {
                    return field;
                }
            }

            throw new InvalidOperationException(
                $"The event stream root field '{responseName}' was not found.");
        }

        private static string ResolveContextPath(EventStreamExecutionNode node)
        {
            var builder = new StringBuilder();
            var lastField = default(string);

            for (var i = 0; i < node._target.Length; i++)
            {
                var segment = node._target[i];
                if (segment.Kind is not SelectionPathSegmentKind.Field)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('.');
                }

                builder.Append(segment.Name);
                lastField = segment.Name;
            }

            if (!string.Equals(lastField, node.FieldName, StringComparison.Ordinal))
            {
                if (builder.Length > 0)
                {
                    builder.Append('.');
                }

                builder.Append(node.FieldName);
            }

            return builder.ToString();
        }

        private static string FormatTopicValue(IValueNode value)
            => value switch
            {
                StringValueNode stringValue => stringValue.Value,
                EnumValueNode enumValue => enumValue.Value,
                IntValueNode intValue => intValue.Value,
                FloatValueNode floatValue => floatValue.Value,
                BooleanValueNode booleanValue => booleanValue.Value ? "true" : "false",
                NullValueNode => "null",
                _ => value.ToString(indented: false)
            };

        private static string? ResolveCursor(
            string? cursorArgument,
            SubscriptionFieldContext subscriptionContext)
        {
            if (string.IsNullOrEmpty(cursorArgument)
                || !subscriptionContext.Arguments.TryGetValue(cursorArgument, out var value)
                || value is NullValueNode)
            {
                return null;
            }

            Debug.Assert(
                value is StringValueNode,
                "The event stream cursor argument is guaranteed to be a string by GraphQL validation.");
            return ((StringValueNode)value).Value;
        }

        private static SourceResultDocument DecodeMessage(
            IMemoryArena arena,
            EventMessage message,
            string fieldName,
            string? cursorField)
        {
            using var buffer = new ArenaBufferWriter(arena);
            var writer = new JsonWriter(buffer, s_eventMessageWriterOptions);

            writer.WriteStartObject();
            writer.WritePropertyName("data"u8);
            writer.WriteStartObject();
            writer.WritePropertyName(fieldName);
            WriteEventPayload(writer, message, cursorField);
            writer.WriteEndObject();
            writer.WriteEndObject();

            return SourceResultDocument.ParseFilled(
                arena,
                buffer.Segments,
                buffer.UsedChunks,
                buffer.LastLength);
        }

        private static void WriteEventPayload(
            JsonWriter writer,
            EventMessage message,
            string? cursorField)
        {
            if (string.IsNullOrEmpty(cursorField))
            {
                writer.WriteRawValue(message.Body);
                return;
            }

            var reader = new Utf8JsonReader(message.Body, isFinalBlock: true, state: default);
            using var document = JsonDocument.ParseValue(ref reader);

            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    "The event stream message body must be a JSON object when a cursor field is configured.");
            }

            writer.WriteStartObject();

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.NameEquals(cursorField))
                {
                    continue;
                }

                writer.WritePropertyName(property.Name);
                WriteRawJsonValue(writer, property.Value);
            }

            writer.WritePropertyName(cursorField);

            if (message.Cursor.IsEmpty)
            {
                writer.WriteNullValue();
            }
            else
            {
                WriteCursorValue(writer, message.Cursor);
            }

            writer.WriteEndObject();
        }

        private static void WriteCursorValue(JsonWriter writer, ReadOnlySpan<byte> cursor)
        {
            var byteCount = cursor.Length + 2;
            byte[]? rented = null;
            var buffer = byteCount <= 256
                ? stackalloc byte[byteCount]
                : rented = ArrayPool<byte>.Shared.Rent(byteCount);

            try
            {
                buffer[0] = (byte)'"';
                cursor.CopyTo(buffer[1..]);
                buffer[byteCount - 1] = (byte)'"';
                writer.WriteRawValue(buffer[..byteCount]);
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        private static void WriteRawJsonValue(JsonWriter writer, JsonElement element)
        {
            var rawText = element.GetRawText();
            var byteCount = Encoding.UTF8.GetByteCount(rawText);
            byte[]? rented = null;
            var buffer = byteCount <= 256
                ? stackalloc byte[byteCount]
                : rented = ArrayPool<byte>.Shared.Rent(byteCount);

            try
            {
                var written = Encoding.UTF8.GetBytes(rawText, buffer);
                writer.WriteRawValue(buffer[..written]);
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }
    }

    private sealed class BrokerSubscription(
        EventStreamSource source,
        IEventStreamBroker broker,
        IAsyncEnumerator<EventMessage> enumerator)
        : IAsyncDisposable
    {
        private bool _disposed;

        public EventStreamSource Source { get; } = source;

        public IEventStreamBroker Broker { get; } = broker;

        public IAsyncEnumerator<EventMessage> Enumerator { get; } = enumerator;

        public SubscriptionArenaSource ArenaSource { get; } = new();

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await Enumerator.DisposeAsync().ConfigureAwait(false);
            await Broker.DisposeAsync().ConfigureAwait(false);
        }
    }
}
