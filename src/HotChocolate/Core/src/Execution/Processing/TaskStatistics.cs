using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    internal class TaskStatistics : ITaskStatistics
    {
        private readonly EventArgs _empty = EventArgs.Empty;

        private int _allTasks;
        private int _newTasks;
        private int _completedTasks;

        public event EventHandler<EventArgs>? StateChanged;

        public event EventHandler<EventArgs>? AllTasksCompleted;

        public int AllTasks => _allTasks;

        public int NewTasks => _newTasks;

        public int RunningTasks => _allTasks - _completedTasks - _newTasks;

        public int CompletedTasks => _completedTasks;

        public bool IsCompleted { get; private set; }

        public void TaskCreated()
        {
            IsCompleted = false;
            Interlocked.Increment(ref _allTasks);
            Interlocked.Increment(ref _newTasks);
            StateChanged?.Invoke(this, _empty);
        }

        public void TaskStarted()
        {
            Interlocked.Decrement(ref _newTasks);
            StateChanged?.Invoke(this, _empty);
        }

        public void TaskCompleted()
        {
            var allTasks = _allTasks;
            var completedTasks = Interlocked.Increment(ref _completedTasks);
            StateChanged?.Invoke(this, _empty);

            if (allTasks == completedTasks)
            {
                IsCompleted = true;
                AllTasksCompleted?.Invoke(this, _empty);
            }
        }

        public void Clear()
        {
            _newTasks = 0;
            _allTasks = 0;
            _completedTasks = 0;
            IsCompleted = false;
        }
    }


    public class RequestTaskScheduler : TaskScheduler
    {
        private int _queuedTasks = 0;

        public event EventHandler<EventArgs> QueueEmpty;

        public bool HasEmptyQueue => _queuedTasks == 0;

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            throw new NotImplementedException();
        }

        protected override void QueueTask(Task task)
        {
            Interlocked.Increment(ref _queuedTasks);
            ThreadPool.UnsafeQueueUserWorkItem(ExecuteTask, task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return task.IsCompleted || TryExecuteTask(task);
        }

        private void ExecuteTask(object task)
        {
            if (Interlocked.Decrement(ref _queuedTasks) == 0)
            {
                QueueEmpty(this, EventArgs.Empty);
            }

            TryExecuteTask((Task)task);
        }

    }
}
