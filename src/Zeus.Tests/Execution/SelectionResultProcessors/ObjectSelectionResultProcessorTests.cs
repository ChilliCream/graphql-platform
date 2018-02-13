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
    public class ObjectSelectionResultProcessorTests
    {
        [Fact]
        public async Task ResultIsFuncThatReturnsNull()
        {
            // arrange
            object result = null;
            bool raised = false;
            object parent = null;

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
            ObjectSelectionResultProcessor selectionResultProcessor = new ObjectSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.Null(result);
            Assert.Empty(nextTasks);
            Assert.Null(parent);
        }

        [Fact]
        public async Task ResultIsFunc()
        {
            // arrange
            object result = null;
            bool raised = false;
            object input = new object();
            object parent = null;

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);
            Mock<IResolver> resolver = new Mock<IResolver>(MockBehavior.Strict);
            resolver.Setup(t => t.ResolveAsync(It.IsAny<IResolverContext>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<object>(new Func<object>(() => input)));

            Mock<IOptimizedSelection> childSelection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            childSelection.Setup(t => t.CreateContext(It.IsAny<IResolverContext>(), It.IsAny<object>()))
                .Returns(new Func<IResolverContext, object, IResolverContext>((c, p) =>
                {
                    parent = p;
                    return resolverContext.Object;
                }));

            Mock<IOptimizedSelection> selection = new Mock<IOptimizedSelection>(MockBehavior.Strict);

            selection.Setup(t => t.Name).Returns("foo");
            selection.Setup(t => t.Resolver).Returns(resolver.Object);
            selection.Setup(t => t.Selections).Returns(new[] { childSelection.Object });

            Action<object> addValueToResultMap = r =>
            {
                result = r;
                raised = true;
            };

            ResolveSelectionTask task = new ResolveSelectionTask(resolverContext.Object, selection.Object, addValueToResultMap);
            await task.ExecuteAsync(CancellationToken.None);

            // act
            ObjectSelectionResultProcessor selectionResultProcessor = new ObjectSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.NotNull(result);
            Assert.IsType<Dictionary<string, object>>(result);
            Assert.Single(nextTasks);
            Assert.Equal(input, parent);
        }

        [Fact]
        public async Task ResultIsObject()
        {
            // arrange
            object result = null;
            bool raised = false;
            object input = new object();

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);
            Mock<IResolver> resolver = new Mock<IResolver>(MockBehavior.Strict);
            resolver.Setup(t => t.ResolveAsync(It.IsAny<IResolverContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(input);

            Mock<IOptimizedSelection> childSelection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            childSelection.Setup(t => t.CreateContext(It.IsAny<IResolverContext>(), It.IsAny<object>()))
                .Returns(resolverContext.Object);

            Mock<IOptimizedSelection> selection = new Mock<IOptimizedSelection>(MockBehavior.Strict);

            selection.Setup(t => t.Name).Returns("foo");
            selection.Setup(t => t.Resolver).Returns(resolver.Object);
            selection.Setup(t => t.Selections).Returns(new[] { childSelection.Object });

            Action<object> addValueToResultMap = r =>
            {
                result = r;
                raised = true;
            };

            ResolveSelectionTask task = new ResolveSelectionTask(resolverContext.Object, selection.Object, addValueToResultMap);
            await task.ExecuteAsync(CancellationToken.None);

            // act
            ObjectSelectionResultProcessor selectionResultProcessor = new ObjectSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.NotNull(result);
            Assert.IsType<Dictionary<string, object>>(result);
            Assert.Single(nextTasks);
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
            ObjectSelectionResultProcessor selectionResultProcessor = new ObjectSelectionResultProcessor();
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
            ObjectSelectionResultProcessor selectionResultProcessor = new ObjectSelectionResultProcessor();
            Action a = () => selectionResultProcessor.Process(null).ToArray();

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }
    }
}