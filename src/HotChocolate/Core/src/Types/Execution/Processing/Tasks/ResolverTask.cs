using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed partial class ResolverTask(ObjectPool<ResolverTask> objectPool) : IResolverTask
{
    private readonly MiddlewareContext _context = new();
    private readonly List<IExecutionTask> _taskBuffer = [];
    private readonly Dictionary<string, ArgumentValue> _args =
        new Dictionary<string, ArgumentValue>(StringComparer.Ordinal);
    private OperationContext _operationContext = null!;
    private Selection _selection = null!;
    private ExecutionTaskStatus _completionStatus = ExecutionTaskStatus.Completed;
    private ExecutionGroupScope? _groupScope;
    private int _groupSlot = -1;
    private bool _childTasksRegistered;

    /// <summary>
    /// Gets or sets the internal execution id.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets the execution branch identifier this task belongs to.
    /// Used by the defer coordinator to track which deferred execution branch
    /// this task contributes results to.
    /// </summary>
    public int BranchId { get; private set; }

    /// <summary>
    /// Gets the primary defer usage that caused this execution branch to be created.
    /// Used to determine whether child tasks should create new branches when their
    /// primary defer usage differs from this one.
    /// </summary>
    internal DeferUsage? DeferUsage { get; private set; }

    /// <summary>
    /// Gets access to the resolver context for this task.
    /// </summary>
    internal MiddlewareContext Context => _context;

    /// <summary>
    /// Gets the execution group scope that this task takes part in, or <c>null</c> if it is not part of a group.
    /// </summary>
    internal ExecutionGroupScope? GroupScope => _groupScope;

    /// <summary>
    /// Gets the slot of this task in its execution group scope, or <c>-1</c> if the task only leads to members.
    /// </summary>
    internal int GroupSlot => _groupSlot;

    /// <summary>
    /// Gets a value indicating whether this task is a member of an execution group.
    /// </summary>
    internal bool IsGroupMember => _groupScope is not null && _groupSlot >= 0;

    /// <summary>
    /// Gets access to the diagnostic events.
    /// </summary>
    private IExecutionDiagnosticEvents DiagnosticEvents => _operationContext.DiagnosticEvents;

    /// <summary>
    /// Gets the selection for which a resolver is executed.
    /// </summary>
    internal Selection Selection => _selection;

    /// <inheritdoc />
    public ExecutionTaskKind Kind
        => _selection.Strategy switch
        {
            SelectionExecutionStrategy.Default => ExecutionTaskKind.Parallel,
            SelectionExecutionStrategy.Serial => ExecutionTaskKind.Serial,
            SelectionExecutionStrategy.Pure => IsGroupMember ? ExecutionTaskKind.Parallel : ExecutionTaskKind.Pure,
            SelectionExecutionStrategy.Batch => ExecutionTaskKind.Parallel,
            _ => throw new NotSupportedException()
        };

    /// <inheritdoc />
    public ExecutionTaskStatus Status { get; private set; }

    /// <inheritdoc />
    public IExecutionTask? Next { get; set; }

    /// <inheritdoc />
    public IExecutionTask? Previous { get; set; }

    /// <inheritdoc />
    public object? State { get; set; }

    /// <inheritdoc />
    public bool IsSerial { get; set; }

    /// <inheritdoc />
    public bool IsRegistered { get; set; }

    /// <inheritdoc />
    public bool IsDeferred => DeferUsage is not null;

    /// <inheritdoc />
    public SelectionPath FieldSelectionPath => Selection.FieldSelectionPath;

    /// <inheritdoc />
    public void BeginExecute(CancellationToken cancellationToken)
    {
#pragma warning disable CA2012
        Status = ExecutionTaskStatus.Running;
        _ = ExecuteAsync(cancellationToken);
#pragma warning restore CA2012
    }
}
