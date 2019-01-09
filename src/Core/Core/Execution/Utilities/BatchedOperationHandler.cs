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
                batchOperation.BufferedRequests += BatchSizeIncreased;
            }
        }

        public async Task CompleteAsync(
            IReadOnlyCollection<Task> tasks,
            CancellationToken cancellationToken)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            if (tasks.Count == 0 || tasks.All(IsFinished))
            {
                return;
            }

            SubscribeToTasks(tasks);
            await InvokeBatchOperationsAsync(cancellationToken);

            if (tasks.Any(IsInProgress))
            {
                _completed = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                StartCompleteTask(tasks, cancellationToken);
                await _completed.Task;
            }
        }

        private void StartCompleteTask(
            IReadOnlyCollection<Task> tasks,
            CancellationToken cancellationToken)
        {
            Task.Run(() => CompleteTasksAsync(tasks, cancellationToken));
        }

        private async Task CompleteTasksAsync(
            IReadOnlyCollection<Task> tasks,
            CancellationToken cancellationToken)
        {
            try
            {
                while (tasks.Any(IsInProgress))
                {
                    await _processSync.WaitAsync(cancellationToken);
                    await InvokeBatchOperationsAsync(cancellationToken);
                }
            }
            finally
            {
                _completed.SetResult(true);
                _touched = new HashSet<IBatchOperation>();
            }
        }

        private async Task InvokeBatchOperationsAsync(
            CancellationToken cancellationToken)
        {
            foreach (IBatchOperation batchOperation in
                await GetTouchedOperationsAsync(cancellationToken))
            {
                if (batchOperation.BufferSize > 0)
                {
                    await batchOperation.InvokeAsync(
                        cancellationToken);
                }
            }
        }

        private async Task<HashSet<IBatchOperation>> GetTouchedOperationsAsync(
            CancellationToken cancellationToken)
        {
            await _touchedSync.WaitAsync(cancellationToken);

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

        private void SubscribeToTasks(
            IReadOnlyCollection<Task> tasks)
        {
            foreach (Task task in tasks)
            {
                task.GetAwaiter().OnCompleted(() =>
                {
                    lock (_processSync)
                    {
                        if (_processSync.CurrentCount == 0)
                        {
                            _processSync.Release();
                        }
                    }
                });
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

                if (_processSync.CurrentCount == 0)
                {
                    _processSync.Release();
                }
            }
        }

        private bool IsInProgress(Task task) => !IsFinished(task);

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
                        batchOperation.BufferedRequests -= BatchSizeIncreased;
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
