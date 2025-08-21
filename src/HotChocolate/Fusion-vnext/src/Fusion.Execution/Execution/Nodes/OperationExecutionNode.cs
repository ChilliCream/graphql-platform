using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Disposables;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Extensions;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationExecutionNode : ExecutionNode
{
    private readonly OperationRequirement[] _requirements;
    private readonly string[] _forwardedVariables;
    private readonly string[] _responseNames;
    private readonly OperationSourceText _operation;
    private readonly string _schemaName;
    private readonly SelectionPath _target;
    private readonly SelectionPath _source;

    internal OperationExecutionNode(
        int id,
        OperationSourceText operation,
        string schemaName,
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
    public OperationSourceText Op => _operation;

    /// <summary>
    /// Gets the response names of the <see cref="Target"/> selection set that are fulfilled by this operation.
    /// </summary>
    public ReadOnlySpan<string> ResponseNames => _responseNames;

    /// <summary>
    /// Gets the name of the source schema that this operation is executed against.
    /// </summary>
    public string SchemaName => _schemaName;

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
        var variables = context.CreateVariableValueSets(_target, _forwardedVariables, _requirements);

        if (variables.Length == 0 && (_requirements.Length > 0 || _forwardedVariables.Length > 0))
        {
            return ExecutionStatus.Skipped;
        }

        var request = new SourceSchemaClientRequest
        {
            OperationType = _operation.Type,
            OperationSourceText = _operation.SourceText,
            Variables = variables
        };

        var client = context.GetClient(_schemaName, _operation.Type);
        SourceSchemaClientResponse response;

        try
        {
            response = await client.ExecuteAsync(context, request, cancellationToken);
        }
        catch (Exception exception)
        {
            AddErrors(context, exception, variables, _responseNames);
            return ExecutionStatus.Failed;
        }

        var index = 0;
        var bufferLength = Math.Max(variables.Length, 1);
        var buffer = ArrayPool<SourceSchemaResult>.Shared.Rent(bufferLength);

        try
        {
            await foreach (var result in response.ReadAsResultStreamAsync(cancellationToken))
            {
                buffer[index++] = result;
            }

            context.AddPartialResults(_source, buffer.AsSpan(0, index), _responseNames);
        }
        catch (Exception exception)
        {
            // if there is an error, we need to make sure that the pooled buffers for the JsonDocuments
            // are returned to the pool.
            foreach (var result in buffer.AsSpan(0, index))
            {
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                result?.Dispose();
            }

            AddErrors(context, exception, variables, _responseNames);

            return ExecutionStatus.Failed;
        }
        finally
        {
            buffer.AsSpan(0, index).Clear();
            ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
        }

        return ExecutionStatus.Success;
    }

    protected override IDisposable CreateScope(OperationPlanContext context)
        => context.GetDiagnosticEvents().ExecuteOperation(context, this);

    internal async Task<SubscriptionResult> SubscribeAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var variables = context.CreateVariableValueSets(_target, _forwardedVariables, _requirements);

        var request = new SourceSchemaClientRequest
        {
            OperationType = _operation.Type,
            OperationSourceText = _operation.SourceText,
            Variables = variables
        };

        var client = context.GetClient(SchemaName, _operation.Type);

        try
        {
            var response = await client.ExecuteAsync(context, request, cancellationToken);

            var stream = new SubscriptionEnumerable(
                context,
                this,
                response,
                response.ReadAsResultStreamAsync(cancellationToken),
                context.GetDiagnosticEvents());

            return SubscriptionResult.Success(stream);
        }
        catch (Exception exception)
        {
            AddErrors(context, exception, variables, _responseNames);

            return SubscriptionResult.Failed();
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
        private readonly SourceSchemaClientResponse _response;
        private readonly IAsyncEnumerable<SourceSchemaResult> _resultEnumerable;
        private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;

        public SubscriptionEnumerable(
            OperationPlanContext context,
            OperationExecutionNode node,
            SourceSchemaClientResponse response,
            IAsyncEnumerable<SourceSchemaResult> resultEnumerable,
            IFusionExecutionDiagnosticEvents diagnosticEvents)
        {
            _context = context;
            _node = node;
            _response = response;
            _resultEnumerable = resultEnumerable;
            _diagnosticEvents = diagnosticEvents;
        }

        public IAsyncEnumerator<EventMessageResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
            => new SubscriptionEnumerator(
                _context,
                _node,
                _response,
                _resultEnumerable.GetAsyncEnumerator(cancellationToken),
                _diagnosticEvents,
                cancellationToken);
    }

    private sealed class SubscriptionEnumerator : IAsyncEnumerator<EventMessageResult>
    {
        private readonly OperationPlanContext _context;
        private readonly OperationExecutionNode _node;
        private readonly SourceSchemaClientResponse _response;
        private readonly IAsyncEnumerator<SourceSchemaResult> _resultEnumerator;
        private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
        private readonly CancellationToken _cancellationToken;
        private readonly SourceSchemaResult[] _resultBuffer = new SourceSchemaResult[1];
        private bool _completed;
        private bool _disposed;

        public SubscriptionEnumerator(
            OperationPlanContext context,
            OperationExecutionNode node,
            SourceSchemaClientResponse response,
            IAsyncEnumerator<SourceSchemaResult> resultEnumerator,
            IFusionExecutionDiagnosticEvents diagnosticEvents,
            CancellationToken cancellationToken)
        {
            _context = context;
            _node = node;
            _response = response;
            _resultEnumerator = resultEnumerator;
            _diagnosticEvents = diagnosticEvents;
            _cancellationToken = cancellationToken;
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

                scope = _diagnosticEvents.ExecuteSubscriptionEvent(_context, _node);
                start = Stopwatch.GetTimestamp();

                if (hasResult)
                {
                    _resultBuffer[0] = _resultEnumerator.Current;
                    _context.AddPartialResults(_node._source, _resultBuffer, _node._responseNames);
                }
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
                    Exception: exception);

                var error = ErrorBuilder.FromException(exception).Build();

                _context.AddErrors(error, _node._responseNames, Path.Root);

                return true;
            }

            if (hasResult)
            {
                Current = new EventMessageResult(
                    _node.Id,
                    Activity.Current,
                    ExecutionStatus.Success,
                    scope,
                    start.Value,
                    Stopwatch.GetTimestamp());
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
        }
    }
}
