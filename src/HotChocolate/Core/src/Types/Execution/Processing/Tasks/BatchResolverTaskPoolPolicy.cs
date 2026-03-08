using Microsoft.Extensions.ObjectPool;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed class BatchResolverTaskPoolPolicy(
    ObjectPool<ResolverTask> resolverTaskPool,
    ObjectPool<Dictionary<string, ArgumentValue>> argumentMapPool) : ExecutionTaskPoolPolicy<BatchResolverTask>
{
    public override BatchResolverTask Create(
        ObjectPool<BatchResolverTask> executionTaskPool) =>
        new(executionTaskPool, resolverTaskPool, argumentMapPool);

    public override bool Reset(BatchResolverTask executionTask)
        => executionTask.Reset();
}
