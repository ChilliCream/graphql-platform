using System;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace HotChocolate.Execution.Processing.Internal
{
    public class DeferredWorkQueueTests
    {
        [Fact]
        public void Enqueue_Work()
        {
            // arrange
            var queue = new DeferredWorkQueue();

            // act
            var count = queue.Enqueue(new MockTask());

            // assert
            Assert.Equal(1, queue.Count);
            Assert.Equal(1, count);
            Assert.False(queue.IsEmpty);
        }

        [Fact]
        public void Enqueue_Null()
        {
            // arrange
            var queue = new DeferredWorkQueue();

            // act
            void Error() => queue.Enqueue(null!);

            // assert
            Assert.Throws<ArgumentNullException>(Error);
            Assert.Equal(0, queue.Count);
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public void Enqueue_Multiple()
        {
            // arrange
            var queue = new DeferredWorkQueue();

            // act
            var count1 = queue.Enqueue(new MockTask());
            var count2 = queue.Enqueue(new MockTask());

            // assert
            Assert.Equal(2, queue.Count);
            Assert.Equal(1, count1);
            Assert.Equal(2, count2);
            Assert.False(queue.IsEmpty);
        }

        [Fact]
        public void Dequeue_Work_InOrder()
        {
            // arrange
            var task1 = new MockTask { Id = 1 };
            var task2 = new MockTask { Id = 2 };
            var task3 = new MockTask { Id = 3 };

            var queue = new DeferredWorkQueue();
            queue.Enqueue(task1);
            queue.Enqueue(task2);
            queue.Enqueue(task3);

            // act
            var dequeueSuccess1 = queue.TryDequeue(out IDeferredExecutionTask? dequeuedTask1);
            var count1 = queue.Count;

            var dequeueSuccess2 = queue.TryDequeue(out IDeferredExecutionTask? dequeuedTask2);
            var count2 = queue.Count;

            var dequeueSuccess3 = queue.TryDequeue(out IDeferredExecutionTask? dequeuedTask3);
            var count3 = queue.Count;

            // assert
            Assert.Equal(0, queue.Count);
            Assert.True(queue.IsEmpty);

            Assert.True(dequeueSuccess1);
            Assert.Same(task1, dequeuedTask1);
            Assert.Equal(2, count1);

            Assert.True(dequeueSuccess2);
            Assert.Same(task2, dequeuedTask2);
            Assert.Equal(1, count2);

            Assert.True(dequeueSuccess3);
            Assert.Same(task3, dequeuedTask3);
            Assert.Equal(0, count3);
        }

        [Fact]
        public void Dequeue_Work_When_Empty()
        {
            // arrange
            var queue = new DeferredWorkQueue();

            // act
            var dequeueSuccess1 = queue.TryDequeue(out IDeferredExecutionTask? dequeuedTask1);

            // assert
            Assert.Equal(0, queue.Count);
            Assert.True(queue.IsEmpty);

            Assert.False(dequeueSuccess1);
            Assert.Null(dequeuedTask1);
        }

        private class MockTask : IDeferredExecutionTask
        {
            /// <summary>
            /// For debugging only.
            /// </summary>
            public int Id { get; set; }

            public Task<IQueryResult> ExecuteAsync(IOperationContext operationContext)
            {
                throw new System.NotImplementedException();
            }

            public IDeferredExecutionTask? Next { get; set; }
            public IDeferredExecutionTask? Previous { get; set; }
        }
    }
}
