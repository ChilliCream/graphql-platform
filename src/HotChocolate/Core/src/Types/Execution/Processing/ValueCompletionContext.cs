namespace HotChocolate.Execution.Processing;

internal readonly ref struct ValueCompletionContext
{
    public ValueCompletionContext(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        List<IExecutionTask> tasks,
        int parentBranchId)
        : this(operationContext, resolverContext, tasks, parentBranchId, groupScope: null)
    {
    }

    public ValueCompletionContext(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        List<IExecutionTask> tasks,
        int parentBranchId,
        ExecutionGroupScope? groupScope)
    {
        OperationContext = operationContext;
        ResolverContext = resolverContext;
        Tasks = tasks;
        ParentBranchId = parentBranchId;
        GroupScope = groupScope;
    }

    public OperationContext OperationContext { get; }

    public MiddlewareContext ResolverContext { get; }

    public List<IExecutionTask> Tasks { get; }

    public int ParentBranchId { get; }

    /// <summary>
    /// Gets the execution group scope that applies to the child tasks, or <c>null</c> if they are not group members.
    /// </summary>
    public ExecutionGroupScope? GroupScope { get; }
}
