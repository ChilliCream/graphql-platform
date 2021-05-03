using System;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal class OperationContextPoolPolicy : IPooledObjectPolicy<OperationContext>
    {
        private readonly Func<OperationContext> _factory;

        public OperationContextPoolPolicy(Func<OperationContext> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public OperationContext Create() => _factory();

        public bool Return(OperationContext obj)
        {
            if (!obj.IsInitialized)
            {
                return true;
            }

            // if work related to the operation context has completed or if it never started
            // we can reuse the operation context.
            if (obj.Execution.TaskStats.IsCompleted ||
                (obj.Execution.TaskStats.AllTasks == 0 &&
                    obj.Execution.TaskStats.NewTasks == 0 &&
                    obj.Execution.TaskStats.CompletedTasks == 0))
            {
                obj.Clean();
                return true;
            }

            // we also clean if we cannot reuse the context so that the context is
            // gracefully discarded and can be garbage collected.
            obj.Clean();
            return false;
        }
    }
}
