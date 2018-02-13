using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    public class ObjectListSelectionResultProcessorTests
    {
        [Fact]
        public async Task ResultIsFuncReturnsNull()
        {
            // arrange
            object result = null;
            bool raised = false;

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);
            Mock<IResolver> resolver = new Mock<IResolver>(MockBehavior.Strict);
            resolver.Setup(t => t.ResolveAsync(It.IsAny<IResolverContext>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<object>(new Func<object>(() => null)));

            Mock<IOptimizedSelection> selection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            selection.Setup(t => t.Name).Returns("foo");
            selection.Setup(t => t.Resolver).Returns(resolver.Object);

            Action<object> addValueToResultMap = r =>
            {
                result = r;
                raised = true;
            };

            ResolveSelectionTask task = new ResolveSelectionTask(resolverContext.Object, selection.Object, addValueToResultMap);
            await task.ExecuteAsync(CancellationToken.None);

            // act
            ObjectListSelectionResultProcessor selectionResultProcessor = new ObjectListSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.Null(result);
            Assert.Empty(nextTasks);
        }

        [InlineData(123)]
        [InlineData("abc")]
        [Theory]
        public async Task ResultIsFunc(object input)
        {
            // arrange
            object result = null;
            bool raised = false;

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);
            Mock<IResolver> resolver = new Mock<IResolver>(MockBehavior.Strict);
            resolver.Setup(t => t.ResolveAsync(It.IsAny<IResolverContext>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<object>(new Func<object>(() => input)));

            Mock<IOptimizedSelection> selection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            selection.Setup(t => t.Name).Returns("foo");
            selection.Setup(t => t.Resolver).Returns(resolver.Object);

            Action<object> addValueToResultMap = r =>
            {
                result = r;
                raised = true;
            };

            ResolveSelectionTask task = new ResolveSelectionTask(resolverContext.Object, selection.Object, addValueToResultMap);
            await task.ExecuteAsync(CancellationToken.None);

            // act
            ObjectListSelectionResultProcessor selectionResultProcessor = new ObjectListSelectionResultProcessor();
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
        public async Task ResultIsSingleScalarValue(object input)
        {
            // arrange
            object result = null;
            bool raised = false;

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);
            Mock<IResolver> resolver = new Mock<IResolver>(MockBehavior.Strict);
            resolver.Setup(t => t.ResolveAsync(It.IsAny<IResolverContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(input);

            Mock<IOptimizedSelection> selection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            selection.Setup(t => t.Name).Returns("foo");
            selection.Setup(t => t.Resolver).Returns(resolver.Object);

            Action<object> addValueToResultMap = r =>
            {
                result = r;
                raised = true;
            };

            ResolveSelectionTask task = new ResolveSelectionTask(resolverContext.Object, selection.Object, addValueToResultMap);
            await task.ExecuteAsync(CancellationToken.None);

            // act
            ObjectListSelectionResultProcessor selectionResultProcessor = new ObjectListSelectionResultProcessor();
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
        public async Task ResultIsList()
        {
            // arrange
            object result = null;
            bool raised = false;
            List<int> input = new List<int> { 1, 2, 3 };

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);
            Mock<IResolver> resolver = new Mock<IResolver>(MockBehavior.Strict);
            resolver.Setup(t => t.ResolveAsync(It.IsAny<IResolverContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(input);

            Mock<IOptimizedSelection> selection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            selection.Setup(t => t.Name).Returns("foo");
            selection.Setup(t => t.Resolver).Returns(resolver.Object);

            Action<object> addValueToResultMap = r =>
            {
                result = r;
                raised = true;
            };

            ResolveSelectionTask task = new ResolveSelectionTask(resolverContext.Object, selection.Object, addValueToResultMap);
            await task.ExecuteAsync(CancellationToken.None);

            // act
            ObjectListSelectionResultProcessor selectionResultProcessor = new ObjectListSelectionResultProcessor();
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

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);
            Mock<IOptimizedSelection> selection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            Action<object> addValueToResultMap = r =>
            {
                result = r;
                raised = true;
            };

            ResolveSelectionTask task = new ResolveSelectionTask(resolverContext.Object, selection.Object, addValueToResultMap);

            // act
            ObjectListSelectionResultProcessor selectionResultProcessor = new ObjectListSelectionResultProcessor();
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
            ObjectListSelectionResultProcessor selectionResultProcessor = new ObjectListSelectionResultProcessor();
            Action a = () => selectionResultProcessor.Process(null).ToArray();

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }
    }
}