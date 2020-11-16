using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Fetching;
using Moq;
using Xunit;

#nullable enable

namespace HotChocolate.Execution.Utilities
{
    public class ExecutionContextTests
    {
        [Fact]
        public void Initialize_TaskStats_ShouldBeSet()
        {
            // act
            ExecutionContext context = CreateExecutionContext();

            // assert
            Assert.NotNull(context.TaskStats);
        }

        [Fact]
        public void Initialize_Tasks_ShouldBeSet()
        {
            // act
            ExecutionContext context = CreateExecutionContext();

            // assert
            Assert.NotNull(context.Tasks);
        }

        [Fact]
        public void Initialize_TaskPool_ShouldBeSet()
        {
            // act
            ExecutionContext context = CreateExecutionContext();

            // assert
            Assert.NotNull(context.TaskPool);
        }

        [Fact]
        public void Initialize_BatchDispatcher_ShouldBeSet()
        {
            // act
            ExecutionContext context = CreateExecutionContext();

            // assert
            Assert.NotNull(context.BatchDispatcher);
        }

        [Fact]
        public void IsCompleted_Should_BeCompleted_When_NoTasksAreRunningOrEnqueued()
        {
            // arrange 
            ExecutionContext context = CreateExecutionContext();

            // assert
            Assert.Equal(0, context.TaskStats.Enqueued);
            Assert.Equal(0, context.TaskStats.Running);
            Assert.True(context.IsCompleted);
        }

        [Fact]
        public void IsCompleted_Should_NotBeCompleted_When_TasksAreEnqued()
        {
            // arrange
            ExecutionContext context = CreateExecutionContext();

            // act
            context.TaskStats.TaskEnqueued();

            //assert
            Assert.NotEqual(0, context.TaskStats.Enqueued);
            Assert.Equal(0, context.TaskStats.Running);
            Assert.False(context.IsCompleted);
        }

        [Fact]
        public void IsCompleted_Should_NotBeCompleted_When_TasksAreRunning()
        {
            // arrange
            ExecutionContext context = CreateExecutionContext();

            // act
            context.TaskStats.TaskStarted();

            //assert
            Assert.Equal(0, context.TaskStats.Enqueued);
            Assert.NotEqual(0, context.TaskStats.Running);
            Assert.False(context.IsCompleted);
        }

        [Fact]
        public void IsCompleted_Should_BeCompleted_When_TasksAreNotLongerEnqued()
        {
            // arrange
            ExecutionContext context = CreateExecutionContext();

            // act
            context.TaskStats.TaskEnqueued();
            context.TaskStats.TaskDequeued();

            //assert
            Assert.Equal(0, context.TaskStats.Enqueued);
            Assert.Equal(0, context.TaskStats.Running);
            Assert.True(context.IsCompleted);
        }

        [Fact]
        public void IsCompleted_Should_BeCompleted_When_TasksAreNotLongeRunning()
        {
            // arrange
            ExecutionContext context = CreateExecutionContext();

            // act
            context.TaskStats.TaskStarted();
            context.TaskStats.TaskCompleted();

            //assert
            Assert.Equal(0, context.TaskStats.Enqueued);
            Assert.Equal(0, context.TaskStats.Running);
            Assert.True(context.IsCompleted);
        }

        [Fact]
        public void WaitForEngine_Should_CompleteImmediately_When_TaskAreEnqued()
        {
            // arrange
            ExecutionContext context = CreateExecutionContext();
            var ct = new CancellationToken();

            // act
            context.TaskStats.TaskEnqueued();

            // start task so does not terminate because there are no tasks running
            context.TaskStats.TaskStarted();

            //assert 
            Assert.Equal(Task.CompletedTask, context.WaitForEngine(ct));
        }

        [Fact]
        public void WaitForEngine_Should_CompleteImmediately_When_BatchSchedulerHasTasks()
        {
            // arrange
            var scheduler = new BatchScheduler();
            ExecutionContext context = CreateExecutionContext(dispatcher: scheduler);
            var ct = new CancellationToken();

            // act
            scheduler.Schedule(() => { });

            //assert 
            Assert.Equal(Task.CompletedTask, context.WaitForEngine(ct));
        }

        [Fact]
        public void WaitForEngine_Should_CompleteImmediately_When_IsCompleted()
        {
            // arrange
            var scheduler = new BatchScheduler();
            ExecutionContext context = CreateExecutionContext(dispatcher: scheduler);
            var ct = new CancellationToken();

            // act 

            //assert 
            Assert.Equal(Task.CompletedTask, context.WaitForEngine(ct));
        }

        [Fact]
        public void WaitForEngine_Should_CompleteeImmediately_When_CancellationIsRequested()
        {
            // arrange
            var scheduler = new BatchScheduler();
            ExecutionContext context = CreateExecutionContext(dispatcher: scheduler);
            var ct = new CancellationTokenSource();

            // act 
            // start task so does not terminate because there are no tasks running
            context.TaskStats.TaskStarted();
            ct.Cancel();
            Task task = context.WaitForEngine(ct.Token);

            //assert  
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void WaitForEngine_Should_CompleteLazily_When_TaskAreEnqued()
        {
            // arrange
            ExecutionContext context = CreateExecutionContext();
            var ct = new CancellationToken();

            // act
            // start task so does not terminate because there are no tasks running
            context.TaskStats.TaskStarted();
            Task task = context.WaitForEngine(ct);
            Assert.False(task.IsCompleted);
            context.TaskStats.TaskEnqueued();

            //assert 
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void WaitForEngine_Should_CompleteLazily_When_BatchSchedulerHasTasks()
        {
            // arrange
            var scheduler = new BatchScheduler();
            ExecutionContext context = CreateExecutionContext(dispatcher: scheduler);
            var ct = new CancellationToken();

            // act 
            // start task so does not terminate because there are no tasks running
            context.TaskStats.TaskStarted();
            Task task = context.WaitForEngine(ct);
            Assert.False(task.IsCompleted);
            scheduler.Schedule(() => { });

            //assert 
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void WaitForEngine_Should_CompleteLazily_When_IsCompleted()
        {
            // arrange
            var scheduler = new BatchScheduler();
            ExecutionContext context = CreateExecutionContext(dispatcher: scheduler);
            var ct = new CancellationToken();

            // act 
            // start task so does not terminate because there are no tasks running
            context.TaskStats.TaskStarted();
            Task task = context.WaitForEngine(ct);
            Assert.False(task.IsCompleted);
            context.TaskStats.TaskCompleted();

            //assert 
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void WaitForEngine_Should_CompleteLazily_When_CancellationIsRequested()
        {
            // arrange
            var scheduler = new BatchScheduler();
            ExecutionContext context = CreateExecutionContext(dispatcher: scheduler);
            var ct = new CancellationTokenSource();

            // act 
            // start task so does not terminate because there are no tasks running
            context.TaskStats.TaskStarted();
            Task task = context.WaitForEngine(ct.Token);

            //assert 
            Assert.False(task.IsCanceled);
            ct.Cancel();
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void Clear_Should_Clear_TaskQueue()
        {
            // arrange 
            IServiceProvider services = new ServiceCollection()
                .TryAddResultPool()
                .TryAddResolverTaskPool()
                .TryAddOperationContextPool()
                .BuildServiceProvider();

            ExecutionContext context = CreateExecutionContext();
            var operationContext = services.GetRequiredService<OperationContext>();
            var selection = new Mock<IPreparedSelection>();
            var resultMap = new ResultMap();
            var path = Path.New("");
            ImmutableDictionary<string, object?> contextData =
                ImmutableDictionary<string, object?>.Empty;
            selection.Setup(x => x.Arguments).Returns(default(IPreparedArgumentMap)!);

            // act 
            // start task so does not terminate because there are no tasks running
            context.Tasks.Enqueue(
                operationContext, selection.Object, 0, resultMap, null, path, contextData);

            //assert 
            Assert.Equal(1, context.Tasks.Count);
            context.Reset();
            Assert.Equal(0, context.Tasks.Count);
        }

        [Fact]
        public void Clear_Should_Clear_TaskStats()
        {
            // arrange 
            ExecutionContext context = CreateExecutionContext();

            // act  
            context.TaskStats.TaskEnqueued();
            context.TaskStats.TaskStarted();

            //assert
            Assert.Equal(1, context.TaskStats.Enqueued);
            Assert.Equal(1, context.TaskStats.Running);
            context.Reset();
            Assert.Equal(0, context.TaskStats.Enqueued);
            Assert.Equal(0, context.TaskStats.Running);
        }

        [Fact]
        public void Clear_Should_Cancel_WaitForEngine()
        {
            // arrange 
            ExecutionContext context = CreateExecutionContext();

            // act  
            // start task so does not terminate because there are no tasks running
            context.TaskStats.TaskStarted();
            Task task = context.WaitForEngine(new CancellationToken());

            //assert 
            Assert.False(task.IsCompleted);
            context.Reset();
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void Clear_Should_Clear_WaitForEngine_ForFurtherCalls()
        {
            // arrange 
            ExecutionContext context = CreateExecutionContext();

            // act   
            context.TaskStats.TaskStarted();
            Task task = context.WaitForEngine(new CancellationToken());
            Assert.False(task.IsCompleted);
            context.Reset();
            task = context.WaitForEngine(new CancellationToken());
            Assert.Equal(Task.CompletedTask, task);
        }

        private ExecutionContext CreateExecutionContext(
            TestPool<ResolverTask>? pool = null,
            BufferedObjectPool<ResolverTask>? bufferedPool = null,
            BatchScheduler? dispatcher = null)
        {
            pool ??= new TestPool<ResolverTask>(3, 3);
            bufferedPool ??= new BufferedObjectPool<ResolverTask>(pool);
            dispatcher ??= new BatchScheduler();
            var context = new ExecutionContext(bufferedPool);
            context.Initialize(dispatcher);
            return context;
        }
    }
}
