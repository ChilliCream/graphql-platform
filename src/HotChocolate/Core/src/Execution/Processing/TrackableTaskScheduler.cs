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
        /// <summary>If false, no more work will be accepted and the active processing tasks will stop as soon as possible</summary>
        private bool _running = true;
        /// <summary>This event is triggered whenever the processing comes to a complete stop (no items left in queue, and nothing running)</summary>
        /// <remarks>_lock will not be acquired while this event is triggered (it is possible process is running event during this event)</remarks>
        private event EventHandler? ProcessingHalted;

        /// <summary>Create a new instance that uses the current scheduler to perform the actual work</summary>
        public TrackableTaskScheduler(TaskScheduler underlyingScheduler)
        {
            _underlyingScheduler = underlyingScheduler;
        }

        /// <returns>true if the scheduler has no remaining work (either on queue or in progress)</returns>
        public bool IsIdle
        {
            get
            {
                lock(_lock)
                {
                    return _processingTaskCount == 0 && (_queue.IsEmpty || !_running);
                }
            }
        }

        public async ValueTask WaitTillIdle(CancellationToken? ctx = null)
        {
            TaskCompletionSource<bool> completion = default!;
            lock (_lock)
            {
                if (_processingTaskCount == 0 && (_queue.IsEmpty || !_running))
                {
                    return;
                }

                completion = new TaskCompletionSource<bool>();
                EventHandler completionHandler = default!;
                completionHandler = (source, args) => {
                    if (!completion.Task.IsCompleted)
                    {
                        try
                        {
                            if (_processingTaskCount == 0 && (_queue.IsEmpty || !_running))
                            {
                                completion.SetResult(true);
                            }
                            else if (ctx?.IsCancellationRequested ?? false)
                            {
                                completion.SetCanceled();
                            }
                        }
                        catch(Exception e)
                        {
                            completion.SetException(e);
                        }
                        finally
                        {
                            ProcessingHalted -= completionHandler;
                        }
                    }
                };
                ctx?.Register(() => completionHandler(this, EventArgs.Empty));
                ProcessingHalted += completionHandler;
            }

            await completion.Task.ConfigureAwait(false);
        }

        /// <summary>Mark that no future work should be handled by this scheduler (will stop all processing tasks as soon as possible)
        /// 
        /// </summary>
        public void Complete()
        {
            _running = false;
        }

        protected override IEnumerable<Task>? GetScheduledTasks()
        {
            return _queue;
        }

        protected override void QueueTask(Task task)
        {
            Contract.Assert(task != null, "Infrastructure should have provided a non-null task.");
            Contract.Assert(_running, "After completion no more work should be scheduled");
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
            if (_running && _processingTaskCount < _processingTaskMax && _queue.Count > 0)
            {
                // Launch concurrent task processing, up to the allowed limit
                for (int i = _queue.Count; i > 0 && _processingTaskCount < _processingTaskMax; --i)
                {
                    ++_processingTaskCount;
                    try
                    {
                        var options = TaskCreationOptions.DenyChildAttach | TaskCreationOptions.PreferFairness;
                        var processingTask = new Task(ProcessTasks, default, options);
                        processingTask.Start(_underlyingScheduler);
                    }
                    catch
                    {
                        --_processingTaskCount;
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
                while (_running && stopwatch.ElapsedMilliseconds < _processingTaskTimeout && _queue.TryDequeue(out task))
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
                    if (_processingTaskCount > 0)  --_processingTaskCount;
                    ProcessAsyncIfNecessary();
                }
                if (_processingTaskCount == 0) ProcessingHalted?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
