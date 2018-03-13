using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Prometheus.Resolvers;
using Prometheus.Abstractions;

namespace Prometheus.Execution.SelectionResultProcessors
{
    public class ObjectSelectionResultProcessorTests
    {
        [Fact]
        public void ResultIsFuncThatReturnsNull()
        {
            // arrange
            object result = null;
            bool raised = false;

            Func<object> input = new Func<object>(() => null);
            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(input, resultIntegratedCallback);

            // act
            ObjectSelectionResultProcessor selectionResultProcessor = new ObjectSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.Null(result);
            Assert.Empty(nextTasks);
        }

        [Fact]
        public void ResultIsFunc()
        {
            // arrange
            object result = null;
            bool raised = false;

            object input = new object();
            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(input, resultIntegratedCallback);

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
        public void ResultIsObject()
        {
            // arrange
            object result = null;
            bool raised = false;

            object input = new object();
            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(input, resultIntegratedCallback);

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

            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(null, resultIntegratedCallback);

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

        private IResolveSelectionTask CreateSelectionTaskMock(object input, Action<object> resultIntegratedCallback)
        {
            return CreateSelectionTaskMock(input, new NamedType("FooType"), resultIntegratedCallback);
        }

        private IResolveSelectionTask CreateSelectionTaskMock(object input, NamedType type, Action<object> resultIntegratedCallback)
        {
            Mock<ISchema> schema = new Mock<ISchema>(MockBehavior.Strict);
            schema.Setup(t => t.InferType(It.IsAny<ObjectTypeDefinition>(),
                It.IsAny<FieldDefinition>(), It.IsAny<object>()))
                .Returns(type);

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);
            resolverContext.Setup(t => t.Schema).Returns(schema.Object);

            ResolverDelegate resolver = new ResolverDelegate((ctx, ct) => Task.FromResult<object>(input));

            Mock<IOptimizedSelection> childSelection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            childSelection.Setup(t => t.CreateContext(It.IsAny<IResolverContext>(), It.IsAny<object>()))
                .Returns(resolverContext.Object);

            Mock<IOptimizedSelection> selection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            selection.Setup(t => t.TypeDefinition).Returns(default(ObjectTypeDefinition));
            selection.Setup(t => t.FieldDefinition).Returns(default(FieldDefinition));
            selection.Setup(t => t.Name).Returns("foo");
            selection.Setup(t => t.Resolver).Returns(resolver);
            selection.Setup(t => t.GetSelections(It.IsAny<IType>())).Returns(new[] { childSelection.Object });

            Action<object> addValueToResultMap = r =>
            {
                resultIntegratedCallback(r);
            };

            ResolveSelectionTask task = new ResolveSelectionTask(resolverContext.Object, selection.Object, addValueToResultMap);
            task.ExecuteAsync(CancellationToken.None).Wait();
            return task;
        }
    }
}