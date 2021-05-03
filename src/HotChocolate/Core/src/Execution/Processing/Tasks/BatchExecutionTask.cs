using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal class BatchExecutionTask : IExecutionTask
    {
        private readonly List<IExecutionTask> _tasks = new();

        public BatchExecutionTask(IOperationContext context)
        {
            Context = context;
        }

        protected IOperationContext Context { get; }

        public ExecutionTaskKind Kind => ExecutionTaskKind.Pure;

        public ICollection<IExecutionTask> Tasks => _tasks;

        public IExecutionTask? Next { get; set; }

        public IExecutionTask? Previous { get; set; }

        public void BeginExecute(CancellationToken cancellationToken)
        {
            foreach (var task in _tasks)
            {
                task.BeginExecute(cancellationToken);
            }
            Context.Execution.TaskBacklog.Complete(this);
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

        public void Reset()
        {
            _tasks.Clear();
            Next = null;
            Previous = null;
        }
    }
}
