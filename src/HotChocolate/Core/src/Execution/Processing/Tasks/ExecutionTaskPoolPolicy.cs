using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal abstract class ExecutionTaskPoolPolicy<T> where T : class, IExecutionTask
    {
        public abstract T Create(ObjectPool<T> executionTaskPool);

        public virtual bool Reset(T executionTask) => true;
    }
}
