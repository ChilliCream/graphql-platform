using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationExecutionNode : ExecutionNode
{
    private readonly OperationRequirement[] _requirements;
    private readonly string[] _forwardedVariables;
    private readonly ResultSelectionSet _resultSelectionSet;
    private readonly ExecutionNodeCondition[] _conditions;
    private readonly bool _requiresFileUpload;
    private readonly OperationSourceText _operation;
    private readonly ulong _operationHash;
    private readonly string? _schemaName;
    private readonly SelectionPath _target;
    private readonly SelectionPath _source;

    internal OperationExecutionNode(
        int id,
        OperationSourceText operation,
        string? schemaName,
        SelectionPath target,
        SelectionPath source,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        bool requiresFileUpload)
    {
        Id = id;
        _operation = operation;
        _operationHash = operation.SourceText.ComputeHash();
        _schemaName = schemaName;
        _target = target;
        _source = source;
        _requirements = requirements;
        _forwardedVariables = forwardedVariables;
        _resultSelectionSet = resultSelectionSet;
        _conditions = conditions;
        _requiresFileUpload = requiresFileUpload;
    }

    /// <inheritdoc />
    public override int Id { get; }

    /// <inheritdoc />
    public override ExecutionNodeType Type => ExecutionNodeType.Operation;

    /// <inheritdoc />
    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => _conditions;

    /// <summary>
    /// Gets the operation definition that this execution node represents.
    /// </summary>
    public OperationSourceText Operation => _operation;

    /// <summary>
    /// Gets the result selection set fulfilled by this operation.
    /// </summary>
    internal ResultSelectionSet ResultSelectionSet => _resultSelectionSet;

    /// <inheritdoc />
    public override string? SchemaName => _schemaName;

    /// <summary>
    /// Gets the path to the selection set for which this operation fetches data.
    /// </summary>
    public SelectionPath Target => _target;

    /// <summary>
    /// Gets the path to the local selection set (the selection set within the source schema request)
    /// to extract the data from.
    /// </summary>
    public SelectionPath Source => _source;

    /// <summary>
    /// Gets the data requirements that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<OperationRequirement> Requirements => _requirements;

    internal ImmutableArray<OperationRequirement> GetRequirementsArray()
        => ImmutableCollectionsMarshal.AsImmutableArray(_requirements);

    /// <summary>
    /// Gets the variables that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<string> ForwardedVariables => _forwardedVariables;

    /// <summary>
    /// Gets whether this operation contains one or more variables
    /// that contain the Upload scalar.
    /// </summary>
    public bool RequiresFileUpload => _requiresFileUpload;

    protected override async ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var diagnosticEvents = context.DiagnosticEvents;
        var variables = context.CreateVariableValueSets(_target, _forwardedVariables, _requirements);

        if (variables.Length == 0 && (_requirements.Length > 0 || _forwardedVariables.Length > 0))
        {
            return ExecutionStatus.Skipped;
        }

        var schemaName = _schemaName ?? context.GetDynamicSchemaName(this);
        context.TrackVariableValueSets(this, variables);

        var request = new SourceSchemaClientRequest
        {
            Node = this,
            SchemaName = schemaName,
            OperationType = _operation.Type,
            OperationSourceText = _operation.SourceText,
            Variables = variables,
            RequiresFileUpload = _requiresFileUpload,
            OperationHash = _operationHash
        };

        var index = 0;
        var bufferLength = 0;
        SourceSchemaResult[]? buffer = null;
        SourceSchemaResult? singleResult = null;
        var hasSomeErrors = false;

        try
        {
            // we execute the GraphQL request against a source schema
            var client = context.GetClient(schemaName, _operation.Type);
            using var clientScope = diagnosticEvents.ExecuteSourceSchemaRequest(context, this, schemaName);

            // we read the responses from the response stream.
            var initialBufferLength = Math.Max(variables.Length, 2);

            await foreach (var result in client.ExecuteAsync(context, request, cancellationToken).ConfigureAwait(false))
            {
                // If there is only one response, we skip the buffer rental.
                if (index == 0)
                {
                    singleResult = result;
                    index = 1;
                }
                else
                {
                    // If we have more than one response, we rent a buffer and move the first result into it.
                    if (buffer is null)
                    {
                        bufferLength = initialBufferLength;
                        buffer = ArrayPool<SourceSchemaResult>.Shared.Rent(bufferLength);
                        buffer[0] = singleResult!;
                    }

                    buffer[index++] = result;
                }

                // Parsing errors here allows the result store to reuse the cached value
                // and avoids a second document lookup per result.
                if (result.Errors is not null)
                {
                    hasSomeErrors = true;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // If the execution of the node was cancelled, either the entire request was cancelled
            // or the execution was halted. In both cases we do not want to produce any errors
            // and just exit the node as quickly as possible.
            return ExecutionStatus.Failed;
        }
        catch (Exception exception)
        {
            diagnosticEvents.SourceSchemaTransportError(context, this, schemaName, exception);

            // if there is an error, we need to make sure that the pooled buffers for the JsonDocuments
            // are returned to the pool.
            if (buffer is not null && bufferLength > 0)
            {
                foreach (var result in buffer.AsSpan(0, index))
                {
                    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    result?.Dispose();
                }

                buffer.AsSpan(0, index).Clear();
                ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
            }
            else
            {
                singleResult?.Dispose();
            }

            context.AddErrors(exception, variables, _resultSelectionSet);
            return ExecutionStatus.Failed;
        }

        try
        {
            if (buffer is not null)
            {
                context.AddPartialResults(
                    _source,
                    buffer.AsSpan(0, index),
                    _resultSelectionSet,
                    hasSomeErrors);
            }
            else if (singleResult is not null)
            {
                var firstResult = singleResult;
                context.AddPartialResults(
                    _source,
                    MemoryMarshal.CreateReadOnlySpan(ref firstResult, 1),
                    _resultSelectionSet,
                    hasSomeErrors);
            }
            else
            {
                context.AddPartialResults(
                    _source,
                    [],
                    _resultSelectionSet,
                    hasSomeErrors);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // If the execution of the node was cancelled, either the entire request was cancelled
            // or the execution was halted. In both cases we do not want to produce any errors
            // and just exit the node as quickly as possible.
            return ExecutionStatus.Failed;
        }
        catch (Exception exception)
        {
            diagnosticEvents.SourceSchemaStoreError(context, this, schemaName, exception);
            context.AddErrors(exception, variables, _resultSelectionSet);
            return ExecutionStatus.Failed;
        }
        finally
        {
            if (buffer is not null)
            {
                buffer.AsSpan(0, index).Clear();
                ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
            }
        }

        return hasSomeErrors ? ExecutionStatus.PartialSuccess : ExecutionStatus.Success;
    }

    protected override IDisposable CreateScope(OperationPlanContext context)
    {
        var schemaName = _schemaName ?? context.GetDynamicSchemaName(this);
        return context.DiagnosticEvents.ExecuteOperationNode(context, this, schemaName);
    }

    internal SubscriptionResult Subscribe(OperationPlanContext context)
    {
        var variables = context.CreateVariableValueSets(_target, _forwardedVariables, _requirements);

        var schemaName = _schemaName ?? context.GetDynamicSchemaName(this);

        context.TrackVariableValueSets(this, variables);

        var request = new SourceSchemaClientRequest
        {
            Node = this,
            SchemaName = schemaName,
            OperationType = _operation.Type,
            OperationSourceText = _operation.SourceText,
            Variables = variables,
            OperationHash = _operationHash
        };

        var subscriptionId = SubscriptionId.Next();

        try
        {
            var stream = new SubscriptionEnumerable(
                context,
                this,
                subscriptionId,
                request,
                context.DiagnosticEvents);

            return SubscriptionResult.Success(subscriptionId, stream);
        }
        catch (Exception ex)
        {
            context.AddErrors(ex, variables, _resultSelectionSet);
            context.DiagnosticEvents.SourceSchemaTransportError(context, this, schemaName, ex);
            return SubscriptionResult.Failed(subscriptionId, ex);
        }
    }

    private sealed class SubscriptionEnumerable : IAsyncEnumerable<EventMessageResult>
    {
        private readonly OperationPlanContext _context;
        private readonly OperationExecutionNode _node;
        private readonly ulong _subscriptionId;
        private readonly SourceSchemaClientRequest _request;
        private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;

        public SubscriptionEnumerable(
            OperationPlanContext context,
            OperationExecutionNode node,
            ulong subscriptionId,
            SourceSchemaClientRequest request,
            IFusionExecutionDiagnosticEvents diagnosticEvents)
        {
            _context = context;
            _node = node;
            _subscriptionId = subscriptionId;
            _request = request;
            _diagnosticEvents = diagnosticEvents;
        }

        public IAsyncEnumerator<EventMessageResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
            => new SubscriptionEnumerator(
                _context,
                _node,
                _node.SchemaName ?? _context.GetDynamicSchemaName(_node),
                _subscriptionId,
                _request,
                _diagnosticEvents,
                cancellationToken);
    }

    private sealed class SubscriptionEnumerator : IAsyncEnumerator<EventMessageResult>
    {
        private readonly ulong _subscriptionId;
        private readonly OperationPlanContext _context;
        private readonly OperationExecutionNode _node;
        private readonly string _schemaName;
        private readonly IAsyncEnumerator<SourceSchemaResult> _eventEnumerator;
        private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
        private readonly CancellationToken _cancellationToken;
        private readonly IDisposable _subscriptionScope;
        private readonly SourceSchemaResult[] _resultBuffer = new SourceSchemaResult[1];
        private readonly SubscriptionArenaSource _eventArenaSource = new();
        private readonly ISourceSchemaClientScope _clientScope;
        private bool _completed;
        private bool _disposed;

        public SubscriptionEnumerator(
            OperationPlanContext context,
            OperationExecutionNode node,
            string schemaName,
            ulong subscriptionId,
            SourceSchemaClientRequest request,
            IFusionExecutionDiagnosticEvents diagnosticEvents,
            CancellationToken cancellationToken)
        {
            _context = context;
            _node = node;
            _schemaName = schemaName;
            _subscriptionId = subscriptionId;
            _diagnosticEvents = diagnosticEvents;
            _cancellationToken = cancellationToken;
            _subscriptionScope = diagnosticEvents.ExecuteSubscription(context.RequestContext, _subscriptionId);

            _clientScope = context.RequestContext.CreateClientScope();
            _eventEnumerator = _clientScope.GetClient(schemaName, request.OperationType)
                .SubscribeAsync(context, request, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
        }

        public EventMessageResult Current { get; private set; } = null!;

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_completed || _cancellationToken.IsCancellationRequested)
            {
                Current = null!;
                return false;
            }

            bool hasResult;
            var received = false;
            IDisposable? scope = null;
            long? start = null;

            try
            {
                _context.SetActiveEventArenaSource(_eventArenaSource);
                hasResult = await _eventEnumerator.MoveNextAsync();

                // From here the event arena is owned by this enumerator: the source has marked it
                // transferred and it finally will no longer dispose it. If anything between here and
                // the event arena being bound and registered as the active arena throws, the arena
                // must be disposed on the failure path below.
                received = hasResult;
                scope = _diagnosticEvents.ExecuteSubscriptionNode(_context, _node, _schemaName, _subscriptionId);
                start = Stopwatch.GetTimestamp();

                if (hasResult)
                {
                    _resultBuffer[0] = _eventEnumerator.Current;

                    // Bind the event arena as the active arena before adding the event's result, so the
                    // event document and the result built for it share one arena and that arena travels
                    // with the delivered result. No event ever carries a second arena. This runs inside
                    // the try so a failure while binding the arena disposes it on the failure path
                    // instead of leaving it to the finalizer.
                    _context.SetActiveEventArena(_eventArenaSource.Arena);

                    _context.AddPartialResults(_node._source, _resultBuffer, _node._resultSelectionSet, containsErrors: true);

                    Current = new EventMessageResult(
                        _node.Id,
                        Activity.Current,
                        ExecutionStatus.Success,
                        scope,
                        start.Value,
                        Stopwatch.GetTimestamp(),
                        Exception: null,
                        VariableValueSets: _context.GetVariableValueSets(_node));
                    return true;
                }
            }
            catch (Exception exception)
            {
                // An event was received but its result was never delivered, so dispose both the parsed
                // result document (returning its pooled tracking arrays) and the arena that backs it
                // here. Both disposals are idempotent, so they are safe whether or not the failure
                // happened after the arena was already registered as the active event arena.
                if (received)
                {
                    _eventEnumerator.Current.Dispose();
                    ((IDisposable)_eventArenaSource.Arena).Dispose();
                }

                Current = new EventMessageResult(
                    _node.Id,
                    Activity.Current,
                    ExecutionStatus.Failed,
                    scope ?? Disposable.Empty,
                    start ?? Stopwatch.GetTimestamp(),
                    Stopwatch.GetTimestamp(),
                    Exception: exception,
                    VariableValueSets: _context.GetVariableValueSets(_node));

                var error = ErrorBuilder.FromException(exception).Build();
                _context.DiagnosticEvents.SubscriptionEventError(
                    _context,
                    _node,
                    _node.SchemaName ?? _context.GetDynamicSchemaName(_node),
                    _subscriptionId,
                    exception);
                _context.AddErrors(error, _node._resultSelectionSet);
                return false;
            }

            _completed = true;
            Current = null!;
            return false;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await _eventEnumerator.DisposeAsync();
            _subscriptionScope.Dispose();
            await _clientScope.DisposeAsync();
        }
    }

    private static class SubscriptionId
    {
        private static ulong s_subscriptionId;

        public static ulong Next() => Interlocked.Increment(ref s_subscriptionId);
    }
}
