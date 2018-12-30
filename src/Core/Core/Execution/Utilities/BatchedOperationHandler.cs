using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal class BatchOperationHandler
        : IDisposable
    {
        private readonly SemaphoreSlim _processSync = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim _touchedSync = new SemaphoreSlim(1, 1);
        private readonly IBatchOperation[] _batchOperations;
        private HashSet<IBatchOperation> _touched =
            new HashSet<IBatchOperation>();
        private TaskCompletionSource<bool> _completed =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public BatchOperationHandler(
            IEnumerable<IBatchOperation> batchOperations)
        {
            if (batchOperations == null)
            {
                throw new ArgumentNullException(nameof(batchOperations));
            }

            _batchOperations = batchOperations.ToArray();

            foreach (IBatchOperation batchOperation in _batchOperations)
            {
                batchOperation.BatchSizeIncreased += BatchSizeIncreased;
            }
        }

        public Task CompleteAsync(
            IReadOnlyCollection<Task> tasks,
            CancellationToken cancellationToken)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            if (tasks.Count == 0)
            {
                return Task.CompletedTask;
            }

            _completed = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            Task.Run(() => CompleteTasksAsync(tasks, cancellationToken));

            return _completed.Task;
        }

        private async Task CompleteTasksAsync(
            IReadOnlyCollection<Task> tasks,
            CancellationToken cancellationToken)
        {
            try
            {
                SubscribeToTasks(tasks, () =>
                {
                    _processSync.Release();
                });

                while (tasks.Any(t => !IsFinished(t)))
                {
                    await _processSync.WaitAsync(cancellationToken);

                    foreach (IBatchOperation batchOperation in
                        GetTouchedOperations())
                    {
                        if (batchOperation.BatchSize > 0)
                        {
                            await batchOperation.InvokeAsync(
                                cancellationToken);
                        }
                    }
                }
            }
            finally
            {
                _completed.SetResult(true);
                _touched = new HashSet<IBatchOperation>();
            }
        }

        private HashSet<IBatchOperation> GetTouchedOperations()
        {
            _touchedSync.Wait();

            try
            {
                HashSet<IBatchOperation> touched = _touched;
                _touched = new HashSet<IBatchOperation>();
                return touched;
            }
            finally
            {
                _touchedSync.Release();
            }
        }

        private static void SubscribeToTasks(
            IReadOnlyCollection<Task> tasks,
            Action finished)
        {
            foreach (Task task in tasks)
            {
                task.GetAwaiter().OnCompleted(finished);
            }
        }

        private void BatchSizeIncreased(object sender, EventArgs args)
        {
            IBatchOperation operation = (IBatchOperation)sender;

            _touchedSync.Wait();

            try
            {
                _touched.Add(operation);
            }
            finally
            {
                _touchedSync.Release();
                _processSync.Release();
            }
        }

        private bool IsFinished(Task task)
        {
            return task.IsCanceled || task.IsCompleted || task.IsFaulted;
        }

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _processSync.Dispose();
                    _touchedSync.Dispose();

                    foreach (IBatchOperation batchOperation in _batchOperations)
                    {
                        batchOperation.BatchSizeIncreased -= BatchSizeIncreased;
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
