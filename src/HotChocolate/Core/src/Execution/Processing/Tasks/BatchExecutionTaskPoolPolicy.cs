using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal sealed class BatchExecutionTaskPoolPolicy : ExecutionTaskPoolPolicy<BatchExecutionTask>
    {
        public override BatchExecutionTask Create(
            ObjectPool<BatchExecutionTask> executionTaskPool) =>
            new();

        public override bool Reset(BatchExecutionTask executionTask)
        {
            executionTask.Reset();
            return true;
        }
    }
}
