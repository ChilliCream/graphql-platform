namespace HotChocolate.Execution.Processing;

internal readonly ref struct ValueCompletionContext
{
    public ValueCompletionContext(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        List<IExecutionTask> tasks,
        int parentBranchId)
    {
        OperationContext = operationContext;
        ResolverContext = resolverContext;
        Tasks = tasks;
        ParentBranchId = parentBranchId;
    }

    public OperationContext OperationContext { get; }

    public MiddlewareContext ResolverContext { get; }

    public List<IExecutionTask> Tasks { get; }

    public int ParentBranchId { get; }
}
