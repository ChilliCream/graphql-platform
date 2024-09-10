using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing;

internal readonly ref struct ValueCompletionContext
{
    public ValueCompletionContext(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        List<ResolverTask> tasks)
    {
        OperationContext = operationContext;
        ResolverContext = resolverContext;
        Tasks = tasks;
    }

    public OperationContext OperationContext { get; }

    public MiddlewareContext ResolverContext { get; }

    public List<ResolverTask> Tasks { get; }
}
