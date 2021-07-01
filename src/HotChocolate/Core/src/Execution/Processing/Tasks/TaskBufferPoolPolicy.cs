using System;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal sealed class TaskBufferPoolPolicy : IPooledObjectPolicy<IExecutionTask?[]>
    {
        public IExecutionTask?[] Create()
        {
            return new IExecutionTask[4];
        }

        public bool Return(IExecutionTask?[] obj)
        {
            obj.AsSpan().Clear();
            return true;
        }
    }
}
