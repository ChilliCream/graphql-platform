using System;
using System.Threading;

namespace HotChocolate.Execution.Processing
{
    internal class TaskStatistics : ITaskStatistics
    {
        private readonly EventArgs _empty = EventArgs.Empty;

        private int _allTasks;
        private int _newTasks;
        private int _completedTasks;
        private int _suspendCompletionEvent;

        public event EventHandler<EventArgs>? StateChanged;

        public event EventHandler<EventArgs>? AllTasksCompleted;

        public int AllTasks => _allTasks;

        public int NewTasks => _newTasks;

        public int RunningTasks => _allTasks - _completedTasks - _newTasks;

        public int CompletedTasks => _completedTasks;

        public bool IsCompleted { get; private set; }

        public void TaskCreated()
        {
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

            if (allTasks == completedTasks && _suspendCompletionEvent == 0)
            {
                IsCompleted = true;
                AllTasksCompleted?.Invoke(this, _empty);
            }
        }

        public void SuspendCompletionEvent()
        {
            Interlocked.Increment(ref _suspendCompletionEvent);
        }

        public void ResumeCompletionEvent()
        {
            if (0 == Interlocked.Decrement(ref _suspendCompletionEvent) && !IsCompleted && _allTasks == _completedTasks)
            {
                IsCompleted = true;
                AllTasksCompleted?.Invoke(this, _empty);
            }
        }

        public void Clear()
        {
            if (_suspendCompletionEvent != 0 || (_allTasks != 0 && !IsCompleted))
            {
                throw new InvalidOperationException("task statistics should not be cleared while still in progress");
            }
            _newTasks = 0;
            _allTasks = 0;
            _completedTasks = 0;
            IsCompleted = false;
        }
    }
}
