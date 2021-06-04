using System.Threading;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using HotChocolate.Execution.Processing.Plan;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using Xunit;

namespace HotChocolate.Execution.Processing.Internal
{
    public class SuspendedWorkQueueTests
    {
        [Fact]
        public void Enqueue_One()
        {
            // arrange
            var queue = new SuspendedWorkQueue();
            var task = new MockExecutionTask();

            // act
            queue.Enqueue(task);

            // assert
            Assert.False(queue.IsEmpty);
        }

        [Fact]
        public void Enqueue_And_Copy()
        {
            // arrange
            var queue = new SuspendedWorkQueue();
            var task1 = new MockExecutionTask();
            var task2 = new MockExecutionTask();

            // act
            queue.Enqueue(task1);
            queue.Enqueue(task2);

            // assert
            var work = new WorkQueue();
            var plan = new QueryPlan(new MockQueryPlanStep());
            var operationContext = new Mock<IOperationContext>();
            var stateMachine = new QueryPlanStateMachine();
            stateMachine.Initialize(operationContext.Object, plan);
            queue.CopyTo(work, work, stateMachine);

            Assert.True(work.TryTake(out IExecutionTask task));
            Assert.Same(task1, task);
            Assert.True(work.TryTake(out task));
            Assert.Same(task2, task);
        }

        internal class MockQueryPlanStep : QueryPlanStep
        {
            public override bool IsPartOf(IExecutionTask task) => true;
        }

        public class MockExecutionTask : IExecutionTask
        {
            public ExecutionTaskKind Kind { get; }
            public bool IsCompleted { get; }
            public IExecutionTask Parent { get; set; }
            public IExecutionTask Next { get; set; }
            public IExecutionTask Previous { get; set; }
            public object State { get; set; }
            public bool IsSerial { get; set; }

            public void BeginExecute(CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            public Task WaitForCompletionAsync(CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
