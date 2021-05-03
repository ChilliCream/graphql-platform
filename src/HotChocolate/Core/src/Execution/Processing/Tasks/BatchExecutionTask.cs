using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal class BatchExecutionTask : IExecutionTask
    {
        private readonly List<IExecutionTask> _tasks = new();
        private IOperationContext _operationContext = default!;

        public ExecutionTaskKind Kind => ExecutionTaskKind.Pure;

        public IExecutionTask? Parent { get; set; }

        public IExecutionTask? Next { get; set; }

        public IExecutionTask? Previous { get; set; }

        public void BeginExecute(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var task in _tasks)
                {
                    task.BeginExecute(cancellationToken);
                }
            }
            finally
            {
                _operationContext.Execution.Work.Complete(this);
            }
        }

        public async Task WaitForCompletionAsync(CancellationToken cancellationToken)
        {
            foreach (var task in _tasks)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await task.WaitForCompletionAsync(cancellationToken);
            }
        }

        public void AddExecutionTask(IExecutionTask executionTask)
        {
            if (executionTask is null)
            {
                throw new ArgumentNullException(nameof(executionTask));
            }

            executionTask.Parent = this;
            _tasks.Add(executionTask);
        }

        public void Initialize(IOperationContext operationContext)
        {
            _operationContext = operationContext;
        }

        public void Reset()
        {
            _tasks.Clear();
            Parent = null;
            Next = null;
            Previous = null;
        }
    }
}
