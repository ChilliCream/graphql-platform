namespace HotChocolate.Execution.Processing;

internal readonly ref struct ValueCompletionContext
{
    public ValueCompletionContext(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        List<IExecutionTask> tasks)
    {
        OperationContext = operationContext;
        ResolverContext = resolverContext;
        Tasks = tasks;
    }

    public OperationContext OperationContext { get; }

    public MiddlewareContext ResolverContext { get; }

    public List<IExecutionTask> Tasks { get; }
}
