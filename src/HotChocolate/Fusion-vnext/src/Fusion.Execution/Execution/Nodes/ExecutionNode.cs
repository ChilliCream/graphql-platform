using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public abstract class ExecutionNode : IEquatable<ExecutionNode>
{
    private bool _isSealed;
    private ExecutionNode[] _dependents = [];
    private ExecutionNode[] _dependencies = [];
    private int _dependentCount;
    private int _dependencyCount;

    /// <summary>
    /// The unique id of this execution node.
    /// </summary>
    public abstract int Id { get; }

    /// <summary>
    /// The type of this execution node.
    /// </summary>
    public abstract ExecutionNodeType Type { get; }

    /// <summary>
    /// The conditions that need to be met to execute this node.
    /// </summary>
    public abstract ReadOnlySpan<ExecutionNodeCondition> Conditions { get; }

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
            if (IsSkipped(context))
            {
                status = ExecutionStatus.Skipped;
            }
            else
            {
                status = await OnExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            OnError(context, scope, ex);
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
            context.GetVariableValueSets(this),
            context.GetTransportDetails(this));

        context.CompleteNode(result);
    }

    protected abstract ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default);

    protected virtual IDisposable? CreateScope(OperationPlanContext context) => null;

    protected virtual void OnError(OperationPlanContext context, IDisposable? scope, Exception error)
        => context.DiagnosticEvents.ExecutionNodeError(context, this, error);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSkipped(OperationPlanContext context)
    {
        if (Conditions.IsEmpty)
        {
            return false;
        }

        ref var condition = ref MemoryMarshal.GetReference(Conditions);
        ref var end = ref Unsafe.Add(ref condition, Conditions.Length);

        while (Unsafe.IsAddressLessThan(ref condition, ref end))
        {
            if (!context.Variables.TryGetValue<BooleanValueNode>(condition.VariableName, out var booleanValueNode))
            {
                throw new InvalidOperationException(
                    $"Expected to have a boolean value for variable '${condition.VariableName}'");
            }

            if (booleanValueNode.Value != condition.PassingValue)
            {
                return true;
            }

            condition = ref Unsafe.Add(ref condition, 1)!;
        }

        return false;
    }
}
