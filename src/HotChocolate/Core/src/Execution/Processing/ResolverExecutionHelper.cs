using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    internal static class ResolverExecutionHelper
    {
        public static Task ExecuteTasksAsync(
            IOperationContext operationContext)
        {
            if (operationContext.Execution.TaskBacklog.IsEmpty)
            {
                return Task.CompletedTask;
            }

            var taskContext = new TaskContext(operationContext);
            return taskContext.ExecuteAsync();
        }

        private sealed class TaskContext
        {
            private readonly IOperationContext _context;
            private int _tasks;

            public TaskContext(IOperationContext context)
            {
                _context = context;

                context.Execution.TaskBacklog.NeedsMoreWorker += (_, _) =>
                {
                    if (Interlocked.Increment(ref _tasks) < 5)
                    {
#pragma warning disable 4014
                        // ExecuteResolversAsync(
                        //    context.Execution,
                        //    HandleError,
                        //    context.RequestAborted,
                        //    false);
#pragma warning restore 4014

                    }
                };
            }

            public Task ExecuteAsync()
            {
                return ExecuteResolversAsync(
                    _context.Execution,
                    HandleError,
                    _context.RequestAborted,
                    true);
            }

            private async Task ExecuteResolversAsync(
                IExecutionContext executionContext,
                Action<Exception> handleError,
                CancellationToken cancellationToken,
                bool mainProcessor)
            {
                if (!mainProcessor)
                {
                    await Task.Yield();
                }

                try
                {
                    do
                    {
                        try
                        {
                            while (!cancellationToken.IsCancellationRequested &&
                                executionContext.TaskBacklog.TryTake(out IExecutionTask? task))
                            {
                                task.BeginExecute(cancellationToken);
                            }

                            if (mainProcessor)
                            {
                                // sync processing


                                if (_tasks == 0 &&
                                    executionContext.TaskBacklog.IsEmpty &&
                                    executionContext.IsCompleted)
                                {
                                    // disable tasks
                                    // enqueue noop
                                    break;
                                }

                                // await executionContext.TaskBacklog
                                //    .WaitForTaskAsync(cancellationToken)
                                //    .ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                handleError(ex);
                            }
                        }
                    }

                    // if this is not the main processing task we will only do one iteration and
                    // then finish since there is not anymore enough work for multiple tasks.
                    while (!cancellationToken.IsCancellationRequested &&
                           mainProcessor &&
                        (!executionContext.IsCompleted ||
                        !executionContext.TaskBacklog.IsEmpty ||
                         _tasks != 0));
                }
                finally
                {
                    if (!mainProcessor)
                    {
                        Interlocked.Decrement(ref _tasks);
                        _context.Execution.TaskBacklog.Register(new NoOpTask());
                    }
                }
            }

            private void HandleError(Exception exception)
            {
                IError error =
                    _context.ErrorHandler
                        .CreateUnexpectedError(exception)
                        .SetCode(ErrorCodes.Execution.TaskProcessingError)
                        .Build();

                error = _context.ErrorHandler.Handle(error);

                // TODO : this error needs to be reported!
                _context.Result.AddError(error);
            }
        }
    }

    internal sealed class NoOpTask : PureExecutionTask
    {
        protected override void Execute(CancellationToken cancellationToken)
        {
        }
    }
}
