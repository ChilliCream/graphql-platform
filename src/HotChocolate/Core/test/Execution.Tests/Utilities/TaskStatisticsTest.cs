using System;
using HotChocolate.Execution.Utilities;
using Xunit;

namespace HotChocolate.Execution
{
    public class TaskStatisticsTest
    {
        [Fact]
        public void DefaultEnqueued()
        {
            // arrange
            var statistics = new TaskStatistics();

            // act && assert  
            Assert.Equal(0, statistics.Enqueued);
        }

        [Fact]
        public void DefaultRunning()
        {
            // arrange
            var statistics = new TaskStatistics();

            // act && assert  
            Assert.Equal(0, statistics.Running);
        }

        [Fact]
        public void Enqueued_Increment()
        {
            // arrange
            var statistics = new TaskStatistics();

            // act && assert
            statistics.TaskEnqueued();
            Assert.Equal(1, statistics.Enqueued);
            statistics.TaskEnqueued();
            Assert.Equal(2, statistics.Enqueued);
        }

        [Fact]
        public void Enqueued_Decrement()
        {
            // arrange
            var statistics = new TaskStatistics();

            // act 
            statistics.TaskEnqueued();
            statistics.TaskEnqueued();

            // assert
            statistics.TaskDequeued();
            Assert.Equal(1, statistics.Enqueued);
            statistics.TaskDequeued();
            Assert.Equal(0, statistics.Enqueued);
        }

        [Fact]
        public void Running_Increment()
        {
            // arrange
            var statistics = new TaskStatistics();

            // act && assert
            statistics.TaskStarted();
            Assert.Equal(1, statistics.Running);
            statistics.TaskStarted();
            Assert.Equal(2, statistics.Running);
        }

        [Fact]
        public void Running_Decrement()
        {
            // arrange
            var statistics = new TaskStatistics();

            // act 
            statistics.TaskStarted();
            statistics.TaskStarted();

            // assert
            statistics.TaskCompleted();
            Assert.Equal(1, statistics.Running);
            statistics.TaskCompleted();
            Assert.Equal(0, statistics.Running);
        }

        [Fact]
        public void Enqueued_Event()
        {
            // arrange
            var statistics = new TaskStatistics();
            var hasBeenCalled = false;

            // act 
            statistics.StateChanged += (object sender, EventArgs args) => hasBeenCalled = true;
            statistics.TaskEnqueued();

            //assert
            Assert.True(hasBeenCalled, "The event for TaskStatistics TaskEnqueued was not called");
        }

        [Fact]
        public void Dequeued_Event()
        {
            // arrange
            var statistics = new TaskStatistics();
            var hasBeenCalled = false;

            // act 
            statistics.StateChanged += (object sender, EventArgs args) => hasBeenCalled = true;
            statistics.TaskDequeued();

            //assert
            Assert.True(hasBeenCalled, "The event for TaskStatistics TaskDequeued was not called");
        }

        [Fact]
        public void Started_Event()
        {
            // arrange
            var statistics = new TaskStatistics();
            var hasBeenCalled = false;

            // act 
            statistics.StateChanged += (object sender, EventArgs args) => hasBeenCalled = true;
            statistics.TaskStarted();

            //assert
            Assert.True(hasBeenCalled, "The event for TaskStatistics TaskStarted was not called");
        }

        [Fact]
        public void Completed_Event()
        {
            // arrange
            var statistics = new TaskStatistics();
            var hasBeenCalled = false;

            // act 
            statistics.StateChanged += (object sender, EventArgs args) => hasBeenCalled = true;
            statistics.TaskCompleted();

            //assert
            Assert.True(hasBeenCalled, "The event for TaskStatistics TaskCompleted was not called");
        }

        [Fact]
        public void RunningTask_Clear_Should_Reset()
        {
            // arrange
            var statistics = new TaskStatistics();

            // act 
            statistics.TaskStarted();
            statistics.TaskStarted();

            // assert
            Assert.Equal(2, statistics.Running);
            statistics.Clear();
            Assert.Equal(0, statistics.Running);
        }

        [Fact]
        public void EnqueuedTask_Clear_Should_Reset()
        {
            // arrange
            var statistics = new TaskStatistics();

            // act 
            statistics.TaskEnqueued();
            statistics.TaskEnqueued();

            // assert
            Assert.Equal(2, statistics.Enqueued);
            statistics.Clear();
            Assert.Equal(0, statistics.Enqueued);
        }
    }
}
