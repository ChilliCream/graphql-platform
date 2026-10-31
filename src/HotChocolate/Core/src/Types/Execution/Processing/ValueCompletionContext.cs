namespace HotChocolate.Execution.Processing;

internal readonly ref struct ValueCompletionContext
{
    public ValueCompletionContext(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        List<IExecutionTask> tasks,
        ExecutionGroupScope? groupScope)
    {
        OperationContext = operationContext;
        ResolverContext = resolverContext;
        Tasks = tasks;
        GroupScope = groupScope;
    }

    public OperationContext OperationContext { get; }

    public MiddlewareContext ResolverContext { get; }

    public List<IExecutionTask> Tasks { get; }

    /// <summary>
    /// Gets the execution group scope that applies to the child tasks, or <c>null</c> if they are not group members.
    /// </summary>
    public ExecutionGroupScope? GroupScope { get; }
}
