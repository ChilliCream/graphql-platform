using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed class BatchResolverTaskPoolPolicy(
    ObjectPool<ResolverTask> resolverTaskPool) : ExecutionTaskPoolPolicy<BatchResolverTask>
{
    public override BatchResolverTask Create(
        ObjectPool<BatchResolverTask> executionTaskPool) =>
        new(executionTaskPool, resolverTaskPool);

    public override bool Reset(BatchResolverTask executionTask)
    {
        executionTask.Reset();
        return true;
    }
}
