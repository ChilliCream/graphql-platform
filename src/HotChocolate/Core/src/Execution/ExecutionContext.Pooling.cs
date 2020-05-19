using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;
using HotChocolate.Execution.Utilities;
using Microsoft.Extensions.ObjectPool;
using System;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext : IExecutionContext
    {
        public void Reset()
        {
            _taskQueue.Clear();
            _taskStatistics.Clear();
            ResetTaskSource();
        }
    }
}
