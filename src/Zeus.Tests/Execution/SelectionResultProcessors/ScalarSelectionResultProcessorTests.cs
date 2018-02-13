
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Zeus.Resolvers;
using Xunit;

namespace Zeus.Execution
{
    public class ScalarSelectionResultProcessorTests
    {
        [Fact]
        public async Task ResultIsFuncThatReturnsNull()
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
            ScalarSelectionResultProcessor selectionResultProcessor = new ScalarSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.Null(result);
            Assert.Empty(nextTasks);
        }

        [Fact]
        public async Task ResultIsFunc()
        {
            // arrange
            object result = null;
            bool raised = false;
            object input = new object();

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
            ScalarSelectionResultProcessor selectionResultProcessor = new ScalarSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.Equal(input, result);
            Assert.Empty(nextTasks);
        }

        [Fact]
        public async Task ResultIsString()
        {
            // arrange
            object result = null;
            bool raised = false;
            string input = "123456";

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
            ScalarSelectionResultProcessor selectionResultProcessor = new ScalarSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.Equal(input, result);
            Assert.Empty(nextTasks);
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
            ScalarSelectionResultProcessor selectionResultProcessor = new ScalarSelectionResultProcessor();
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
            ScalarSelectionResultProcessor selectionResultProcessor = new ScalarSelectionResultProcessor();
            Action a = () => selectionResultProcessor.Process(null).ToArray();

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }
    }
}