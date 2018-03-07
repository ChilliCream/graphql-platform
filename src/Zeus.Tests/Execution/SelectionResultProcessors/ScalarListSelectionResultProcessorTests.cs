using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Zeus.Resolvers;
using Zeus.Abstractions;

namespace Zeus.Execution
{
    public class ScalarListSelectionResultProcessorTests
    {
        [Fact]
        public void ResultIsFuncReturnsNull()
        {
            // arrange
            object result = null;
            bool raised = false;

            Func<object> input = () => null;
            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(input, resultIntegratedCallback);

            // act
            ScalarListSelectionResultProcessor selectionResultProcessor = new ScalarListSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.Null(result);
            Assert.Empty(nextTasks);
        }

        [InlineData(123)]
        [InlineData("abc")]
        [Theory]
        public void ResultIsFunc(object input)
        {
            // arrange
            object result = null;
            bool raised = false;

            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(input, resultIntegratedCallback);

            // act
            ScalarListSelectionResultProcessor selectionResultProcessor = new ScalarListSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.NotNull(result);
            Assert.IsType<object[]>(result);
            Assert.Empty(nextTasks);

            object[] list = (object[])result;
            Assert.Contains(input, list);
            Assert.Single(list);
        }



        [InlineData(123)]
        [InlineData("abc")]
        [Theory]
        public void ResultIsSingleScalarValue(object input)
        {
            // arrange
            object result = null;
            bool raised = false;

            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(input, resultIntegratedCallback);

            // act
            ScalarListSelectionResultProcessor selectionResultProcessor = new ScalarListSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.NotNull(result);
            Assert.IsType<object[]>(result);
            Assert.Empty(nextTasks);

            object[] list = (object[])result;
            Assert.Contains(input, list);
            Assert.Single(list);
        }

        [Fact]
        public void ResultIsList()
        {
            // arrange
            object result = null;
            bool raised = false;
            List<int> input = new List<int> { 1, 2, 3 };

            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(input, resultIntegratedCallback);

            // act
            ScalarListSelectionResultProcessor selectionResultProcessor = new ScalarListSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.NotNull(result);
            Assert.IsType<object[]>(result);
            Assert.Empty(nextTasks);

            Assert.Collection((object[])result,
                i => Assert.Equal(1, i),
                i => Assert.Equal(2, i),
                i => Assert.Equal(3, i));
        }

        [Fact]
        public void ResultIsNull()
        {
            // arrange
            object result = null;
            bool raised = false;

            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(null, resultIntegratedCallback);

            // act
            ScalarListSelectionResultProcessor selectionResultProcessor = new ScalarListSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.Null(result);
            Assert.Empty(nextTasks);
        }

        [Fact]
        public void TaskIsNull()
        {
            // act
            ScalarListSelectionResultProcessor selectionResultProcessor = new ScalarListSelectionResultProcessor();
            Action a = () => selectionResultProcessor.Process(null).ToArray();

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        private IResolveSelectionTask CreateSelectionTaskMock(object input, Action<object> resultIntegratedCallback)
        {
            return CreateSelectionTaskMock(input, new NamedType("FooType"), resultIntegratedCallback);
        }

        private IResolveSelectionTask CreateSelectionTaskMock(object input, NamedType type, Action<object> resultIntegratedCallback)
        {
            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);

            ResolverDelegate resolver = new ResolverDelegate((ctx, ct) => Task.FromResult<object>(input));

            Mock<IOptimizedSelection> selection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            selection.Setup(t => t.Name).Returns("foo");
            selection.Setup(t => t.Resolver).Returns(resolver);

            ResolveSelectionTask task = new ResolveSelectionTask(resolverContext.Object, selection.Object, resultIntegratedCallback);
            task.ExecuteAsync(CancellationToken.None).Wait();
            return task;
        }
    }
}