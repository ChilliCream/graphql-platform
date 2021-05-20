using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal sealed class PureResolverTaskPoolPolicy : ExecutionTaskPoolPolicy<PureResolverTask>
    {
        public override PureResolverTask Create(
            ObjectPool<PureResolverTask> executionTaskPool) =>
            new(executionTaskPool);

        public override bool Reset(PureResolverTask executionTask) =>
            executionTask.Reset();
    }
}
