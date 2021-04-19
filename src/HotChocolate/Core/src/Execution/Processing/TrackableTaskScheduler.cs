using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    /// <summary>A task scheduler that allow tracking if work is still in progress</summary>
    /// <remarks>Based on code from QueuedTaskScheduler and ConcurrentExclusiveSchedulerPair</remarks>
    internal class TrackableTaskScheduler : TaskScheduler
    {
        private readonly object _lock = new();
        /// <summary>The queue holding pending tasks</summary>
        private readonly ConcurrentQueue<Task> _queue = new ConcurrentQueue<Task>();
        /// <summary>The underlying task scheduler to which all work should be scheduled.</summary>
        private readonly TaskScheduler _underlyingScheduler;
        /// <summary>The maximum number of tasks processors that can be active at the same time.</summary>
        private readonly int _processingTaskMax = Environment.ProcessorCount;
        /// <summary>The amount of time in milliseconds a single processing task can keep the underlying scheduler occupied</summary>
        /// <remarks>If a task takes longer it will not quit, it will just result in a new processing task after it completes</remarks>
        private readonly long _processingTaskTimeout = 50;
        /// <summary>The actual number of tasks processors that are currently active.</summary>
        private int _processingTaskCount = 0;
        /// <summary>If > 0, no more work will be accepted and the active processing tasks will stop as soon as possible</summary>
        private int _shutdown = 0;
        /// <summary>This event is triggered whenever the processing comes to a complete stop (no items left in queue, and nothing running)</summary>
        private event EventHandler? ProcessingHalted;

        /// <summary>Create a new instance that uses the current scheduler to perform the actual work</summary>
        public TrackableTaskScheduler(TaskScheduler underlyingScheduler)
        {
            _underlyingScheduler = underlyingScheduler;
        }

        /// <returns>true if the scheduler has no remaining work (either on queue or in progress)</returns>
        public bool IsIdle => _processingTaskCount == 0;

        public async Task WaitTillIdle(CancellationToken? ctx = null)
        {
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
            CancellationTokenRegistration? ctxRegistration = ctx?.Register(() => completion.TrySetCanceled());
            EventHandler completionHandler = (source, args) =>
            {
                try
                {
                    if (ctx?.IsCancellationRequested ?? false)
                    {
                        completion.TrySetCanceled();
                    }
                    else
                    {
                        completion.TrySetResult(true);
                    }
                }
                catch (Exception e)
                {
                    completion.TrySetException(e);
                }
            };

            lock (_lock)
            {
                ProcessingHalted += completionHandler;

                if (ctx?.IsCancellationRequested ?? false)
                {
                    completion.TrySetCanceled();
                }
                else if (IsIdle)
                {
                    completion.TrySetResult(true);
                }
            }

            try
            {
                await completion.Task.ConfigureAwait(false);
            }
            finally
            {
                ctxRegistration?.Dispose();
                lock (_lock)
                {
                    ProcessingHalted -= completionHandler;
                }
            }
        }

        /// <summary>Mark that no future work should be handled by this scheduler (will stop all processing tasks as soon as possible)
        /// 
        /// </summary>
        public void Complete()
        {
            Interlocked.Increment(ref _shutdown);
        }

        protected override IEnumerable<Task>? GetScheduledTasks()
        {
            return _queue;
        }

        protected override void QueueTask(Task task)
        {
            Contract.Assert(task != null, "Infrastructure should have provided a non-null task.");
            Contract.Assert(_shutdown == 0, "After completion no more work should be scheduled");
            lock (_lock)
            {
                _queue.Enqueue(task);
                ProcessAsyncIfNecessary();
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return TryExecuteTask(task);
        }

        /// <remarks>This method assumes that the caller takes a lock on _lock</remarks>
        private void ProcessAsyncIfNecessary()
        {
            if (_shutdown == 0 && _processingTaskCount < _processingTaskMax && _queue.Count > 0)
            {
                // Launch concurrent task processing, up to the allowed limit
                for (int i = _queue.Count; i > 0 && _processingTaskCount < _processingTaskMax; --i)
                {
                    Interlocked.Increment(ref _processingTaskCount);
                    try
                    {
                        var options = TaskCreationOptions.DenyChildAttach | TaskCreationOptions.PreferFairness;
                        var processingTask = new Task(ProcessTasks, default, options);
                        processingTask.Start(_underlyingScheduler);
                    }
                    catch
                    {
                        Interlocked.Decrement(ref _processingTaskCount);
                        throw;
                    }
                }
            }
        }

        private void ProcessTasks()
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Task task;
                while (_shutdown == 0 && stopwatch.ElapsedMilliseconds < _processingTaskTimeout && _queue.TryDequeue(out task))
                {
                    // Execute the task. If the scheduler was previously faulted,
                    // this task could have been faulted when it was queued; ignore such tasks.
                    if (!task.IsCompleted)
                    {
                        TryExecuteTask(task);
                        Contract.Assert(task.IsCompleted);
                    }
                }
            }
            finally
            {
                lock(_lock)
                {
                    var value = Interlocked.Decrement(ref _processingTaskCount);
                    if (value < 0)
                    {
                        // should not happen, but auto recover if it does just in case
                        Interlocked.CompareExchange(ref _processingTaskCount, 0, value);
                    }

                    ProcessAsyncIfNecessary();

                    if (_processingTaskCount == 0) ProcessingHalted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
