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
        private readonly object _processSyncLock = new object();
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
            Memory<Task> tasks,
            CancellationToken cancellationToken)
        {
            if (tasks.Length == 0 || All(tasks.Span, IsFinished))
            {
                return;
            }

            SubscribeToTasks(tasks.Span);
            await InvokeBatchOperationsAsync(cancellationToken)
                .ConfigureAwait(false);

            if (Any(tasks.Span, IsInProgress))
            {
                _completed = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                StartCompleteTask(tasks, cancellationToken);
                await _completed.Task.ConfigureAwait(false);
            }
        }

        private void StartCompleteTask(
            Memory<Task> tasks,
            CancellationToken cancellationToken)
        {
            Task.Run(() => CompleteTasksAsync(tasks, cancellationToken));
        }

        private async Task CompleteTasksAsync(
            Memory<Task> tasks,
            CancellationToken cancellationToken)
        {
            try
            {
                while (Any(tasks.Span, IsInProgress))
                {
                    await _processSync.WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                    await InvokeBatchOperationsAsync(cancellationToken)
                        .ConfigureAwait(false);
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
                await GetTouchedOperationsAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                if (batchOperation.BufferSize > 0)
                {
                    await batchOperation.InvokeAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task<HashSet<IBatchOperation>> GetTouchedOperationsAsync(
            CancellationToken cancellationToken)
        {
            await _touchedSync.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

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
            in ReadOnlySpan<Task> tasks)
        {
            foreach (Task task in tasks)
            {
                task.GetAwaiter().OnCompleted(() =>
                {
                    ReleaseProcessSyncIfNeeded();
                });
            }
        }

        private void ReleaseProcessSyncIfNeeded()
        {
            // This can be invoked from task continuations and batch-size
            // callbacks that run on thread-pool threads after the operation has
            // completed. Hold _processSyncLock so the semaphore cannot be
            // disposed mid-release: without this, Dispose() can null the
            // semaphore's internal lock between SemaphoreSlim's dispose check
            // and its Monitor.Pulse call, surfacing as an ArgumentNullException
            // ("obj") on a background thread, which tears down the process on
            // the .NET Framework.
            lock (_processSyncLock)
            {
                if (_disposed)
                {
                    return;
                }

                if (_processSync.CurrentCount == 0)
                {
                    _processSync.Release();
                }
            }
        }

        private void BatchSizeIncreased(object sender, EventArgs args)
        {
            var operation = (IBatchOperation)sender;

            _touchedSync.Wait();

            try
            {
                _touched.Add(operation);
            }
            finally
            {
                _touchedSync.Release();

                ReleaseProcessSyncIfNeeded();
            }
        }

        private static bool All(
            ReadOnlySpan<Task> tasks,
            Func<Task, bool> predicate)
        {
            for (int i = 0; i < tasks.Length; i++)
            {
                if (!predicate(tasks[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool Any(
            ReadOnlySpan<Task> tasks,
            Func<Task, bool> predicate)
        {
            for (int i = 0; i < tasks.Length; i++)
            {
                if (predicate(tasks[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsInProgress(Task task) => !IsFinished(task);

        private static bool IsFinished(Task task)
        {
            return task.IsCanceled || task.IsCompleted || task.IsFaulted;
        }

        #region IDisposable Support

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            // Take the same lock ReleaseProcessSyncIfNeeded uses and flip
            // _disposed before disposing the semaphore, so any in-flight or
            // queued release observes _disposed == true and bails out instead
            // of racing the disposal of _processSync.
            lock (_processSyncLock)
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
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
