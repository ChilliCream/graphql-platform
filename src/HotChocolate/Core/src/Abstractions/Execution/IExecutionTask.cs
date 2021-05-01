using System.Threading;
using System.Threading.Tasks;

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
        /// <summary>
        /// Begins executing this task.
        /// </summary>
        void BeginExecute(CancellationToken cancellationToken);
    }

    public abstract class ExecutionTask : IExecutionTask
    {
        protected abstract IExecutionTaskContext Context { get; }

        public void BeginExecute(CancellationToken cancellationToken)
        {
            Context.Started();
#pragma warning disable 4014
            ExecuteInternalAsync(cancellationToken);
#pragma warning restore 4014
        }

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
                Context.Completed();
            }
        }

        protected abstract ValueTask ExecuteAsync(CancellationToken cancellationToken);
    }

    public abstract class PureExecutionTask : IExecutionTask
    {
        public void BeginExecute(CancellationToken cancellationToken)
        {
            Execute(cancellationToken);
        }

        protected abstract void Execute(CancellationToken cancellationToken);
    }
}
