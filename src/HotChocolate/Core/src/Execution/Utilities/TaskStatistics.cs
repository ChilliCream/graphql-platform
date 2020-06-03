using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Utilities
{
    internal class TaskStatistics : ITaskStatistics
    {
        public object SyncRoot = new object();

        private int _allTasks;
        private int _newTasks;
        private int _runningTasks;
        private int _completedTasks;

        public event EventHandler<EventArgs>? StateChanged;

        public int AllTasks => _allTasks;

        public int NewTasks => _newTasks;

        public int RunningTasks => _runningTasks;

        public int CompletedTasks => _completedTasks;

        public bool IsCompleted => _allTasks == _completedTasks;

        public void TaskCreated()
        {
            lock (SyncRoot)
            {
                _allTasks++;
                _newTasks++;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void TaskStarted()
        {
            lock (SyncRoot)
            {
                _runningTasks++;
                _newTasks--;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void TaskCompleted()
        {
            lock (SyncRoot)
            {
                _completedTasks++;
                _runningTasks--;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Clear()
        {
            _newTasks = 0;
            _allTasks = 0;
            _runningTasks = 0;
            _completedTasks = 0;
        }
    }
}
