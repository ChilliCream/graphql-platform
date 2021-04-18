using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
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
        private readonly int _processingTaskMax = Environment.ProcessorCount + 1;
        /// <summary>The actual number of tasks processors that are currently active.</summary>
        private int _processingTaskCount = 0;
        /// <summary>If false, no more work will be accepted and the active processing tasks will stop as soon as possible</summary>
        private bool _running = true;

        /// <summary>Create a new instance that uses the current scheduler to perform the actual work</summary>
        public TrackableTaskScheduler(TaskScheduler underlyingScheduler)
        {
            _underlyingScheduler = underlyingScheduler;
        }

        /// <returns>true if the scheduler has no remaining work (either on queue or in progress)</returns>
        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return _processingTaskCount == 0 && _queue.IsEmpty;
                }
            }
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

        private void ProcessAsyncIfNecessary(bool isReplacementReplica = false)
        {
            if (_running && _processingTaskCount < _processingTaskMax && _queue.Count > 0)
            {
                // Launch concurrent task processing, up to the allowed limit
                for (int i = _queue.Count; i > 0 && _processingTaskCount < _processingTaskMax; --i)
                {
                    ++_processingTaskCount;
                    try
                    {
                        var options = TaskCreationOptions.DenyChildAttach;
                        if (isReplacementReplica)
                        {
                            options |= TaskCreationOptions.PreferFairness;
                        }
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
                Task task;
                while (_running && _queue.TryDequeue(out task))
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
                    ProcessAsyncIfNecessary(true);
                }
            }
        }
    }
}
