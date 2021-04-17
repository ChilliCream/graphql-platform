using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Execution.Processing
{
    public class TaskStatisticsTests
    {
        [Fact]
        public void Basic()
        {
            int stateChangedCount = 0;
            int allTasksCompletedCount = 0;
            var stats = new TaskStatistics();
            stats.StateChanged += (_,_) => ++stateChangedCount;
            stats.AllTasksCompleted += (_, _) => ++allTasksCompletedCount;
            Assert.False(stats.IsCompleted);
            Assert.Equal(0, stats.AllTasks);
            Assert.Equal(0, stats.NewTasks);
            Assert.Equal(0, stats.RunningTasks);
            Assert.Equal(0, stats.CompletedTasks);
            Assert.Equal(0, stateChangedCount);
            Assert.Equal(0, allTasksCompletedCount);
            stats.TaskCreated();
            Assert.Equal(1, stats.AllTasks);
            Assert.Equal(1, stats.NewTasks);
            Assert.Equal(0, stats.RunningTasks);
            Assert.Equal(0, stats.CompletedTasks);
            Assert.Equal(1, stateChangedCount);
            Assert.Equal(0, allTasksCompletedCount);
            stats.TaskStarted();
            Assert.Equal(1, stats.AllTasks);
            Assert.Equal(0, stats.NewTasks);
            Assert.Equal(1, stats.RunningTasks);
            Assert.Equal(0, stats.CompletedTasks);
            Assert.Equal(2, stateChangedCount);
            Assert.Equal(0, allTasksCompletedCount);
            stats.TaskCompleted();
            Assert.Equal(1, stats.AllTasks);
            Assert.Equal(0, stats.NewTasks);
            Assert.Equal(0, stats.RunningTasks);
            Assert.Equal(1, stats.CompletedTasks);
            Assert.True(stats.IsCompleted);
            Assert.Equal(2, stateChangedCount); // TODO: verify this is intended (no state changed on completion)
            Assert.Equal(1, allTasksCompletedCount);
            stats.Clear();
            Assert.Equal(0, stats.AllTasks);
            Assert.Equal(0, stats.NewTasks);
            Assert.Equal(0, stats.RunningTasks);
            Assert.Equal(0, stats.CompletedTasks);
            Assert.False(stats.IsCompleted);
            Assert.Equal(2, stateChangedCount);
            Assert.Equal(1, allTasksCompletedCount);
        }

        [Fact]
        public void Basic_NoListeners()
        {
            var stats = new TaskStatistics();
            Assert.False(stats.IsCompleted);
            Assert.Equal(0, stats.AllTasks);
            Assert.Equal(0, stats.NewTasks);
            Assert.Equal(0, stats.RunningTasks);
            Assert.Equal(0, stats.CompletedTasks);
            stats.TaskCreated();
            Assert.Equal(1, stats.AllTasks);
            Assert.Equal(1, stats.NewTasks);
            Assert.Equal(0, stats.RunningTasks);
            Assert.Equal(0, stats.CompletedTasks);
            stats.TaskStarted();
            Assert.Equal(1, stats.AllTasks);
            Assert.Equal(0, stats.NewTasks);
            Assert.Equal(1, stats.RunningTasks);
            Assert.Equal(0, stats.CompletedTasks);
            stats.TaskCompleted();
            Assert.Equal(1, stats.AllTasks);
            Assert.Equal(0, stats.NewTasks);
            Assert.Equal(0, stats.RunningTasks);
            Assert.Equal(1, stats.CompletedTasks);
            Assert.True(stats.IsCompleted);
        }

        [Fact]
        public void Suspend()
        {
            int allTasksCompletedCount = 0;
            var stats = new TaskStatistics();
            stats.AllTasksCompleted += (_, _) => ++allTasksCompletedCount;
            stats.TaskCreated();
            stats.TaskStarted();
            Assert.False(stats.IsCompleted);
            stats.SuspendCompletionEvent();
            stats.SuspendCompletionEvent();
            stats.TaskCompleted();
            stats.ResumeCompletionEvent();
            Assert.Equal(0, allTasksCompletedCount);
            Assert.False(stats.IsCompleted);
            stats.ResumeCompletionEvent();
            Assert.Equal(1, allTasksCompletedCount);
            Assert.True(stats.IsCompleted);
            stats.SuspendCompletionEvent();
            stats.ResumeCompletionEvent();
            Assert.Equal(1, allTasksCompletedCount);
            Assert.True(stats.IsCompleted);
        }

        [Fact]
        public void Suspend_NoListeners()
        {
            var stats = new TaskStatistics();
            Assert.False(stats.IsCompleted);
            Assert.Equal(0, stats.AllTasks);
            Assert.Equal(0, stats.NewTasks);
            Assert.Equal(0, stats.RunningTasks);
            Assert.Equal(0, stats.CompletedTasks);
            stats.TaskCreated();
            Assert.Equal(1, stats.AllTasks);
            Assert.Equal(1, stats.NewTasks);
            Assert.Equal(0, stats.RunningTasks);
            Assert.Equal(0, stats.CompletedTasks);
            stats.TaskStarted();
            Assert.Equal(1, stats.AllTasks);
            Assert.Equal(0, stats.NewTasks);
            Assert.Equal(1, stats.RunningTasks);
            Assert.Equal(0, stats.CompletedTasks);
            stats.SuspendCompletionEvent();
            stats.TaskCompleted();
            Assert.Equal(1, stats.AllTasks);
            Assert.Equal(0, stats.NewTasks);
            Assert.Equal(0, stats.RunningTasks);
            Assert.Equal(1, stats.CompletedTasks);
            Assert.False(stats.IsCompleted);
            stats.ResumeCompletionEvent();
            Assert.True(stats.IsCompleted);
        }

        [Fact]
        public void Clear_ExceptionHandling()
        {
            var stats = new TaskStatistics();
            stats.Clear();
            stats.TaskCreated();
            Assert.Throws<InvalidOperationException>(() => stats.Clear());
            Assert.Equal(1, stats.NewTasks);
            stats.TaskStarted();
            Assert.Throws<InvalidOperationException>(() => stats.Clear());
            Assert.Equal(1, stats.AllTasks);
            stats.TaskCompleted();
            Assert.True(stats.IsCompleted);
            stats.SuspendCompletionEvent();
            Assert.Throws<InvalidOperationException>(() => stats.Clear());
            stats.ResumeCompletionEvent();
            stats.Clear();
        }
    }
}
