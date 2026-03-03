namespace HotChocolate.Execution.Processing;

internal readonly ref struct ValueCompletionContext
{
    public ValueCompletionContext(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        List<IExecutionTask> tasks,
        int parentBranchId,
        BatchSelectionPath? parentSelectionPath = null)
    {
        OperationContext = operationContext;
        ResolverContext = resolverContext;
        Tasks = tasks;
        ParentBranchId = parentBranchId;
        ParentSelectionPath = parentSelectionPath;
    }

    public OperationContext OperationContext { get; }

    public MiddlewareContext ResolverContext { get; }

    public List<IExecutionTask> Tasks { get; }

    public int ParentBranchId { get; }

    /// <summary>
    /// Gets the batch selection path of the parent task.
    /// Used to construct child task paths and to key batch resolver lookups.
    /// </summary>
    public BatchSelectionPath? ParentSelectionPath { get; }
}
