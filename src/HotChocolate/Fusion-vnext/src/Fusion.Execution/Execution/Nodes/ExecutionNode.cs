using System.Diagnostics;

namespace HotChocolate.Fusion.Execution.Nodes;

public abstract class ExecutionNode : IEquatable<ExecutionNode>
{
    private bool _isSealed;
    private ExecutionNode[] _dependents = [];
    private ExecutionNode[] _dependencies = [];
    private int _dependentCount;
    private int _dependencyCount;

    public abstract int Id { get; }

    public abstract ExecutionNodeType Type { get; }

    /// <summary>
    /// Gets the execution nodes that depend on this node to be completed
    /// before they can be executed.
    /// </summary>
    public ReadOnlySpan<ExecutionNode> Dependents => _dependents;

    /// <summary>
    /// Gets the execution nodes that this operation depends on.
    /// </summary>
    public ReadOnlySpan<ExecutionNode> Dependencies => _dependencies;

    public async Task ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        var scope = CreateScope(context);
        var activity = Activity.Current;
        ExecutionStatus status;
        Exception? error = null;

        try
        {
            status = await OnExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnError(scope, ex);
            error = ex;
            status = ExecutionStatus.Failed;
        }
        finally
        {
            scope?.Dispose();
        }

        var result = new ExecutionNodeResult(
            Id,
            activity,
            status,
            Stopwatch.GetElapsedTime(start),
            error,
            context.GetDependentsToExecute(this),
            context.GetVariableValueSets(this));

        context.CompleteNode(result);
    }

    protected abstract ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default);

    protected virtual IDisposable? CreateScope(OperationPlanContext context) => null;

    protected virtual void OnError(IDisposable? scope, Exception error) { }

    protected void EnqueueDependentForExecution(OperationPlanContext context, ExecutionNode dependent)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dependent);

        context.EnqueueForExecution(this, dependent);
    }

    internal void AddDependency(ExecutionNode node)
    {
        ExpectMutable();

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
        ExpectMutable();

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

    protected internal void Seal()
    {
        ExpectMutable();

        if (_dependencies.Length > _dependencyCount)
        {
            Array.Resize(ref _dependencies, _dependencyCount);
        }

        if (_dependents.Length > _dependentCount)
        {
            Array.Resize(ref _dependents, _dependentCount);
        }

        OnSealingNode();

        _isSealed = true;
    }

    protected virtual void OnSealingNode()
    {
    }

    protected void ExpectMutable()
    {
        if (_isSealed)
        {
            throw new InvalidOperationException("The operation execution node is already sealed.");
        }
    }

    public bool Equals(ExecutionNode? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj)
        => Equals(obj as ExecutionNode);

    public override int GetHashCode()
        => Id;
}
