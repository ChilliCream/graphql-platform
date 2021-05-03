using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    /*

     Strategy.Parallel => (backlog: 5, max: 5)
     Strategy.Serial
     Strategy.Pure => (backlog: 5, max: 5)

     DataLoader =>

     [ExecutionStrategy(Strategy.Serial)]
     public ValueTask<string> Fo()
     {
     }
     */

    public interface IExecutionTask
    {
        ExecutionTaskKind Kind { get; }

        /// <summary>
        /// Begins executing this task.
        /// </summary>
        void BeginExecute(CancellationToken cancellationToken);

        Task WaitForCompletionAsync(CancellationToken cancellationToken);

        IExecutionTask? Parent { get; set; }

        IExecutionTask? Next { get; set; }

        IExecutionTask? Previous { get; set; }
    }

    public enum ExecutionTaskKind
    {
        Parallel,
        Serial,
        Pure
    }

    public abstract class ExecutionTask : IExecutionTask
    {
        private Task? _task;

        protected abstract IExecutionTaskContext Context { get; }

        public ExecutionTaskKind Kind => ExecutionTaskKind.Parallel;

        public IExecutionTask? Parent { get; set; }

        public IExecutionTask? Next { get; set; }

        public IExecutionTask? Previous { get; set; }

        public void BeginExecute(CancellationToken cancellationToken)
        {
            Context.Started(this);
            _task = ExecuteInternalAsync(cancellationToken).AsTask();
        }

        public Task WaitForCompletionAsync(CancellationToken cancellationToken) =>
            _task ?? Task.CompletedTask;


        private async ValueTask ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (Context.Track(this))
                {
                    await ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                Context.Completed(this);
            }
        }

        protected abstract ValueTask ExecuteAsync(CancellationToken cancellationToken);


    }

    public abstract class PureExecutionTask : IExecutionTask
    {
        public ExecutionTaskKind Kind => ExecutionTaskKind.Pure;

        public IExecutionTask? Next { get; set; }

        public IExecutionTask? Previous { get; set; }

        public IExecutionTask? Parent { get; set; }

        public void BeginExecute(CancellationToken cancellationToken)
        {
            Execute(cancellationToken);
        }

        protected abstract void Execute(CancellationToken cancellationToken);

        public Task WaitForCompletionAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
