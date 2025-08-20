using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Extensions;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationExecutionNode : ExecutionNode
{
    private readonly OperationRequirement[] _requirements;
    private readonly string[] _variables;
    private readonly string[] _responseNames;

    public OperationExecutionNode(
        int id,
        OperationDefinitionNode operation,
        string schemaName,
        SelectionPath target,
        SelectionPath source,
        OperationRequirement[] requirements,
        string[] responseNames)
    {
        Id = id;
        Operation = operation;
        SchemaName = schemaName;
        Target = target;
        Source = source;
        _requirements = requirements;
        _responseNames = responseNames;

        // We compute the hash of the operation definition when it is set.
        // This hash can be used within the GraphQL client to identify the operation
        // and optimize request serialization.
        var operationBytes = Encoding.UTF8.GetBytes(operation.ToString());
#if NET9_0_OR_GREATER
        OperationHash = Convert.ToHexStringLower(SHA256.HashData(operationBytes));
#else
        OperationHash = Convert.ToHexString(SHA256.HashData(operationBytes)).ToLowerInvariant();
#endif

        var variables = new List<string>();

        foreach (var variableDef in operation.VariableDefinitions)
        {
            if (requirements.Any(r => r.Key == variableDef.Variable.Name.Value))
            {
                continue;
            }

            variables.Add(variableDef.Variable.Name.Value);
        }

        _variables = variables.ToArray();
    }

    /// <summary>
    /// Gets the plan unique node id.
    /// </summary>
    public override int Id { get; }

    public override ExecutionNodeType Type => ExecutionNodeType.Operation;

    /// <summary>
    /// Gets the unique identifier of the operation.
    /// </summary>
    public string OperationId => OperationHash;

    /// <summary>
    /// Gets a SHA256 has of the <see cref="Operation"/>.
    /// </summary>
    public string OperationHash { get; }

    /// <summary>
    /// Gets the operation definition that this execution node represents.
    /// </summary>
    public OperationDefinitionNode Operation { get; }

    /// <summary>
    /// Gets the response names of the <see cref="Target"/> selection set that are fulfilled by this operation.
    /// </summary>
    public ReadOnlySpan<string> ResponseNames => _responseNames;

    /// <summary>
    /// Gets the name of the source schema that this operation is executed against.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the path to the selection set for which this operation fetches data.
    /// </summary>
    public SelectionPath Target { get; }

    /// <summary>
    /// Gets the path to the local selection set (the selection set within the source schema request)
    /// to extract the data from.
    /// </summary>
    public SelectionPath Source { get; }

    /// <summary>
    /// Gets the data requirements that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<OperationRequirement> Requirements => _requirements;

    /// <summary>
    /// Gets the variables that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<string> Variables => _variables;

    protected override async ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var variables = context.CreateVariableValueSets(Target, Variables, Requirements);

        if (variables.Length == 0 && (Requirements.Length > 0 || Variables.Length > 0))
        {
            return ExecutionStatus.Skipped;
        }

        var request = new SourceSchemaClientRequest
        {
            OperationId = OperationId,
            Operation = Operation,
            Variables = variables
        };

        var client = context.GetClient(SchemaName, Operation.Operation);
        SourceSchemaClientResponse response;

        try
        {
            response = await client.ExecuteAsync(context, request, cancellationToken);
        }
        catch (Exception exception)
        {
            AddErrors(context, exception, variables, ResponseNames);
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

            context.AddPartialResults(Source, buffer.AsSpan(0, index), ResponseNames);
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

            AddErrors(context, exception, variables, ResponseNames);

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
        var variables = context.CreateVariableValueSets(Target, Variables, Requirements);

        var request = new SourceSchemaClientRequest
        {
            OperationId = OperationId,
            Operation = Operation,
            Variables = variables
        };

        var client = context.GetClient(SchemaName, Operation.Operation);

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
            AddErrors(context, exception, variables, ResponseNames);

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
                    _context.AddPartialResults(_node.Source, _resultBuffer, _node.ResponseNames);
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

                _context.AddErrors(error, _node.ResponseNames, Path.Root);

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
