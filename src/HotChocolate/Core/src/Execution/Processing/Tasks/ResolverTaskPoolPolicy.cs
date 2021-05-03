using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal sealed class ResolverTaskPoolPolicy : ExecutionTaskPoolPolicy<ResolverTask>
    {
        public override ResolverTask Create(
            ObjectPool<ResolverTask> executionTaskPool) =>
            new(executionTaskPool);

        public override bool Reset(ResolverTask executionTask) =>
            executionTask.Reset();
    }
}
