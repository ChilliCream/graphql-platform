using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Disposables;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationExecutionNode : ExecutionNode
{
    private readonly OperationRequirement[] _requirements;
    private readonly string[] _forwardedVariables;
    private readonly string[] _responseNames;
    private readonly OperationSourceText _operation;
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
        string[] responseNames)
    {
        Id = id;
        _operation = operation;
        _schemaName = schemaName;
        _target = target;
        _source = source;
        _requirements = requirements;
        _forwardedVariables = forwardedVariables;
        _responseNames = responseNames;
    }

    /// <summary>
    /// Gets the plan unique node id.
    /// </summary>
    public override int Id { get; }

    /// <summary>
    /// Gets the type of the execution node.
    /// </summary>
    public override ExecutionNodeType Type => ExecutionNodeType.Operation;

    /// <summary>
    /// Gets the operation definition that this execution node represents.
    /// </summary>
    public OperationSourceText Operation => _operation;

    /// <summary>
    /// Gets the response names of the <see cref="Target"/> selection set that are fulfilled by this operation.
    /// </summary>
    public ReadOnlySpan<string> ResponseNames => _responseNames;

    /// <summary>
    /// Gets the name of the source schema that this operation is executed against.
    /// If <c>null</c> the schema is dynamic and will be set at runtime.
    /// </summary>
    public string? SchemaName => _schemaName;

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

    /// <summary>
    /// Gets the variables that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<string> ForwardedVariables => _forwardedVariables;

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
            OperationType = _operation.Type,
            OperationSourceText = _operation.SourceText,
            Variables = variables
        };

        var client = context.GetClient(schemaName, _operation.Type);

        var index = 0;
        var bufferLength = 0;
        SourceSchemaResult[]? buffer = null;
        var hasSomeErrors = false;

        try
        {
            // we execute the GraphQL request against a source schema
            var response = await client.ExecuteAsync(context, request, cancellationToken);

            // we read the responses from the response stream.
            bufferLength = Math.Max(variables.Length, 1);
            buffer = ArrayPool<SourceSchemaResult>.Shared.Rent(bufferLength);

            await foreach (var result in response.ReadAsResultStreamAsync(cancellationToken))
            {
                buffer[index++] = result;

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

            AddErrors(context, exception, variables, _responseNames);
            return ExecutionStatus.Failed;
        }

        try
        {
            context.AddPartialResults(_source, buffer.AsSpan(0, index), _responseNames);
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
            AddErrors(context, exception, variables, _responseNames);
            return ExecutionStatus.Failed;
        }
        finally
        {
            buffer.AsSpan(0, index).Clear();
            ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
        }

        return hasSomeErrors ? ExecutionStatus.PartialSuccess : ExecutionStatus.Success;
    }

    protected override IDisposable CreateScope(OperationPlanContext context)
    {
        var schemaName = _schemaName ?? context.GetDynamicSchemaName(this);
        return context.DiagnosticEvents.ExecuteOperationNode(context, this, schemaName);
    }

    internal async Task<SubscriptionResult> SubscribeAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var variables = context.CreateVariableValueSets(_target, _forwardedVariables, _requirements);

        var schemaName = _schemaName ?? context.GetDynamicSchemaName(this);

        context.TrackVariableValueSets(this, variables);

        var request = new SourceSchemaClientRequest
        {
            OperationType = _operation.Type,
            OperationSourceText = _operation.SourceText,
            Variables = variables
        };

        var client = context.GetClient(schemaName, _operation.Type);
        var subscriptionId = SubscriptionId.Next();

        try
        {
            var response = await client.ExecuteAsync(context, request, cancellationToken);

            var stream = new SubscriptionEnumerable(
                context,
                this,
                subscriptionId,
                response,
                response.ReadAsResultStreamAsync(cancellationToken),
                context.DiagnosticEvents);

            return SubscriptionResult.Success(subscriptionId, stream);
        }
        catch (Exception ex)
        {
            AddErrors(context, ex, variables, _responseNames);
            context.DiagnosticEvents.SubscriptionTransportError(context, this, schemaName, subscriptionId, ex);
            return SubscriptionResult.Failed(subscriptionId, ex);
        }
    }

    private static void AddErrors(
        OperationPlanContext context,
        Exception exception,
        ImmutableArray<VariableValues> variables,
        ReadOnlySpan<string> responseNames)
    {
        var error = ErrorBuilder.FromException(exception).Build();

        if (variables.Length == 0)
        {
            context.AddErrors(error, responseNames, Path.Root);
        }
        else
        {
            var pathBufferLength = variables.Length;
            var pathBuffer = ArrayPool<Path>.Shared.Rent(pathBufferLength);

            try
            {
                for (var i = 0; i < variables.Length; i++)
                {
                    pathBuffer[i] = variables[i].Path;
                }

                context.AddErrors(error, responseNames, pathBuffer.AsSpan(0, pathBufferLength));
            }
            finally
            {
                pathBuffer.AsSpan(0, pathBufferLength).Clear();
                ArrayPool<Path>.Shared.Return(pathBuffer);
            }
        }
    }

    private sealed class SubscriptionEnumerable : IAsyncEnumerable<EventMessageResult>
    {
        private readonly OperationPlanContext _context;
        private readonly OperationExecutionNode _node;
        private readonly ulong _subscriptionId;
        private readonly SourceSchemaClientResponse _response;
        private readonly IAsyncEnumerable<SourceSchemaResult> _resultEnumerable;
        private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;

        public SubscriptionEnumerable(
            OperationPlanContext context,
            OperationExecutionNode node,
            ulong subscriptionId,
            SourceSchemaClientResponse response,
            IAsyncEnumerable<SourceSchemaResult> resultEnumerable,
            IFusionExecutionDiagnosticEvents diagnosticEvents)
        {
            _context = context;
            _node = node;
            _subscriptionId = subscriptionId;
            _response = response;
            _resultEnumerable = resultEnumerable;
            _diagnosticEvents = diagnosticEvents;
        }

        public IAsyncEnumerator<EventMessageResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
            => new SubscriptionEnumerator(
                _context,
                _node,
                _node.SchemaName ?? _context.GetDynamicSchemaName(_node),
                _subscriptionId,
                _response,
                _resultEnumerable.GetAsyncEnumerator(cancellationToken),
                _diagnosticEvents,
                cancellationToken);
    }

    private sealed class SubscriptionEnumerator : IAsyncEnumerator<EventMessageResult>
    {
        private readonly ulong _subscriptionId;
        private readonly OperationPlanContext _context;
        private readonly OperationExecutionNode _node;
        private readonly string _schemaName;
        private readonly SourceSchemaClientResponse _response;
        private readonly IAsyncEnumerator<SourceSchemaResult> _resultEnumerator;
        private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
        private readonly CancellationToken _cancellationToken;
        private readonly IDisposable _subscriptionScope;
        private readonly SourceSchemaResult[] _resultBuffer = new SourceSchemaResult[1];
        private bool _completed;
        private bool _disposed;

        public SubscriptionEnumerator(
            OperationPlanContext context,
            OperationExecutionNode node,
            string schemaName,
            ulong subscriptionId,
            SourceSchemaClientResponse response,
            IAsyncEnumerator<SourceSchemaResult> resultEnumerator,
            IFusionExecutionDiagnosticEvents diagnosticEvents,
            CancellationToken cancellationToken)
        {
            _context = context;
            _node = node;
            _schemaName = schemaName;
            _subscriptionId = subscriptionId;
            _response = response;
            _resultEnumerator = resultEnumerator;
            _diagnosticEvents = diagnosticEvents;
            _cancellationToken = cancellationToken;
            _subscriptionScope = diagnosticEvents.ExecuteSubscription(context.RequestContext, _subscriptionId);
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
            IDisposable? scope = null;
            long? start = null;

            try
            {
                hasResult = await _resultEnumerator.MoveNextAsync();
                scope = _diagnosticEvents.ExecuteSubscriptionNode(_context, _node, _schemaName, _subscriptionId);
                start = Stopwatch.GetTimestamp();
            }
            catch (Exception exception)
            {
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                _resultBuffer[0]?.Dispose();
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
                _context.AddErrors(error, _node._responseNames);
                return false;
            }

            if (hasResult)
            {
                _resultBuffer[0] = _resultEnumerator.Current;
                _context.AddPartialResults(_node._source, _resultBuffer, _node._responseNames);

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

            _completed = true;
            Current = null!;
            return false;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                return;
            }

            _disposed = true;
            _response.Dispose();
            await _resultEnumerator.DisposeAsync();
            _subscriptionScope.Dispose();
        }
    }

    private static class SubscriptionId
    {
        private static ulong s_subscriptionId;

        public static ulong Next() => Interlocked.Increment(ref s_subscriptionId);
    }
}
