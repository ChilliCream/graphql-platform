using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static HotChocolate.Execution.Properties.Resources;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal class BatchExecutionTask : IExecutionTask
    {
        private readonly List<IExecutionTask> _pure = new();
        private readonly List<IExecutionTask> _parallel = new();
        private IOperationContext _operationContext = default!;
        private Task _task = Task.CompletedTask;

        public ExecutionTaskKind Kind { get; private set; } = ExecutionTaskKind.Pure;

        public bool IsCompleted { get; private set; }

        public IExecutionTask? Parent { get; set; }

        public IExecutionTask? Next { get; set; }

        public IExecutionTask? Previous { get; set; }

        public void BeginExecute(CancellationToken cancellationToken)
        {
            try
            {
                if (_pure.Count > 0)
                {
                    ExecutePure(cancellationToken);
                }

                if (_parallel.Count > 0)
                {
                    _task = ExecuteParallelAsync(cancellationToken);
                }
            }
            finally
            {
                if (Kind == ExecutionTaskKind.Pure)
                {
                    IsCompleted = true;
                    _operationContext.Execution.Work.Complete(this);
                }
            }
        }

        private void ExecutePure(CancellationToken cancellationToken)
        {
            for (var i = 0; i < _pure.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _pure[i].BeginExecute(cancellationToken);
            }
        }

        private async Task ExecuteParallelAsync(CancellationToken cancellationToken)
        {
            try
            {
                for (var i = 0; i < _parallel.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    _parallel[i].BeginExecute(cancellationToken);
                }

                for (var i = 0; i < _parallel.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    IExecutionTask task = _parallel[i];

                    if (!task.IsCompleted)
                    {
                        await task.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                IsCompleted = true;
                _operationContext.Execution.Work.Complete(this);
            }
        }

        public Task WaitForCompletionAsync(CancellationToken cancellationToken) => _task;

        public void AddExecutionTask(IExecutionTask executionTask)
        {
            if (executionTask is null)
            {
                throw new ArgumentNullException(nameof(executionTask));
            }

            if (executionTask.Kind == ExecutionTaskKind.Serial)
            {
                throw new NotSupportedException(
                    BatchExecutionTask_AddExecutionTask_SerialTasksNotAllowed);
            }

            executionTask.Parent = this;

            if (executionTask.Kind == ExecutionTaskKind.Parallel)
            {
                Kind = ExecutionTaskKind.Parallel;
                _parallel.Add(executionTask);
            }
            else
            {
                _pure.Add(executionTask);
            }
        }

        public void Initialize(IOperationContext operationContext)
        {
            _operationContext = operationContext;
        }

        public void Reset()
        {
            _parallel.Clear();
            _pure.Clear();
            _task = Task.CompletedTask;
            Kind = ExecutionTaskKind.Pure;
            IsCompleted = false;
            Parent = null;
            Next = null;
            Previous = null;
        }
    }
}
