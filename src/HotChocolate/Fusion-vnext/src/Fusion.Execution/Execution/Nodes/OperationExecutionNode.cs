using System.Buffers;
using System.Diagnostics;
using System.IO.Hashing;
using System.Reactive.Disposables;
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
    private ExecutionNode[] _dependencies = [];
    private ExecutionNode[] _dependents = [];
    private int _dependencyCount;
    private int _dependentCount;
    private bool _isSealed;

    public OperationExecutionNode(
        int id,
        OperationDefinitionNode operation,
        string schemaName,
        SelectionPath target,
        SelectionPath source,
        OperationRequirement[] requirements)
    {
        Id = id;
        Operation = operation;
        SchemaName = schemaName;
        Target = target;
        Source = source;
        _requirements = requirements;

        // We compute the hash of the operation definition when it is set.
        // This hash can be used within the GraphQL client to identify the operation
        // and optimize request serialization.
        OperationId = XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(operation.ToString())).ToString();

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

    public override int Id { get; }

    /// <summary>
    /// Gets the unique identifier of the operation.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Gets the operation definition that this execution node represents.
    /// </summary>
    public OperationDefinitionNode Operation { get; }

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
    /// Gets the execution nodes that depend on this operation to be completed
    /// before they can be executed.
    /// </summary>
    public ReadOnlySpan<ExecutionNode> Dependents => _dependents;

    /// <summary>
    /// Gets the execution nodes that this operation depends on.
    /// </summary>
    public override ReadOnlySpan<ExecutionNode> Dependencies => _dependencies;

    /// <summary>
    /// Gets the data requirements that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<OperationRequirement> Requirements => _requirements;

    /// <summary>
    /// Gets the variables that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<string> Variables => _variables;

    public override async Task<ExecutionNodeResult> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var diagnosticEvents = context.GetDiagnosticEvents();
        using var scope = diagnosticEvents.ExecuteOperation(context, this);
        return await ExecuteInternalAsync(context, cancellationToken);
    }

    private async Task<ExecutionNodeResult> ExecuteInternalAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        var variables = context.CreateVariableValueSets(Target, Variables, Requirements);

        if (variables.Length == 0 && (Requirements.Length > 0 || Variables.Length > 0))
        {
            return new ExecutionNodeResult(
                Id,
                Activity.Current,
                ExecutionStatus.Skipped,
                Stopwatch.GetElapsedTime(start));
        }

        var request = new SourceSchemaClientRequest
        {
            OperationId = OperationId,
            Operation = Operation,
            Variables = variables
        };

        var client = context.GetClient(SchemaName, Operation.Operation);
        var response = await client.ExecuteAsync(context, request, cancellationToken);

        if (!response.IsSuccessful)
        {
            return new ExecutionNodeResult(
                Id,
                Activity.Current,
                ExecutionStatus.Failed,
                Stopwatch.GetElapsedTime(start));
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

            context.AddPartialResults(Source, buffer.AsSpan(0, index));
        }
        catch
        {
            // if there is an error, we need to make sure that the pooled buffers for the JsonDocuments
            // are returned to the pool.
            foreach (var result in buffer.AsSpan(0, index))
            {
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                result?.Dispose();
            }

            throw;
        }
        finally
        {
            buffer.AsSpan(0, index).Clear();
            ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
        }

        return new ExecutionNodeResult(
            Id,
            Activity.Current,
            ExecutionStatus.Success,
            Stopwatch.GetElapsedTime(start));
    }

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
        var response = await client.ExecuteAsync(context, request, cancellationToken);

        if (!response.IsSuccessful)
        {
            return SubscriptionResult.Failed();
        }

        var stream = new SubscriptionEnumerable(
            context,
            this,
            response,
            response.ReadAsResultStreamAsync(cancellationToken),
            context.GetDiagnosticEvents());
        return SubscriptionResult.Success(stream);
    }

    internal void AddDependency(ExecutionNode node)
    {
        if (_isSealed)
        {
            throw new InvalidOperationException("The operation execution node is already sealed.");
        }

        ArgumentNullException.ThrowIfNull(node);

        if (node.Equals(this))
        {
            throw new InvalidOperationException("An operation cannot depend on itself.");
        }

        if (_dependencies.Length == 0)
        {
            _dependencies = new ExecutionNode[4];
        }

        if (_dependencyCount == _dependencies.Length)
        {
            Array.Resize(ref _dependencies, _dependencyCount * 2);
        }

        _dependencies[_dependencyCount++] = node;
    }

    internal void AddDependent(ExecutionNode node)
    {
        if (_isSealed)
        {
            throw new InvalidOperationException("The operation execution node is already sealed.");
        }

        ArgumentNullException.ThrowIfNull(node);

        if (node.Equals(this))
        {
            throw new InvalidOperationException("An operation cannot depend on itself.");
        }

        if (_dependents.Length == 0)
        {
            _dependents = new ExecutionNode[4];
        }

        if (_dependentCount == _dependents.Length)
        {
            Array.Resize(ref _dependents, _dependentCount * 2);
        }

        _dependents[_dependentCount++] = node;
    }

    protected internal override void Seal()
    {
        if (_isSealed)
        {
            throw new InvalidOperationException("The operation execution node is already sealed.");
        }

        if (_dependencies.Length > _dependencyCount)
        {
            Array.Resize(ref _dependencies, _dependencyCount);
        }

        if (_dependents.Length > _dependentCount)
        {
            Array.Resize(ref _dependents, _dependentCount);
        }

        _isSealed = true;
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
                    _context.AddPartialResults(_node.Source, _resultBuffer);
                }
            }
            catch (Exception ex)
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
                    Exception: ex);
                return true;
            }

            if (hasResult)
            {
                Current = new EventMessageResult(
                    _node.Id,
                    Activity.Current,
                    ExecutionStatus.Failed,
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
