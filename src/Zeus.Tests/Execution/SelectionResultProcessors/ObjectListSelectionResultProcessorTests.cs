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
    public class ObjectListSelectionResultProcessorTests
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
        public void ResultIsFunc(object input)
        {
            // arrange
            object result = null;
            bool raised = false;

            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(new Func<object>(() => input), resultIntegratedCallback);

            // act
            ObjectListSelectionResultProcessor selectionResultProcessor = new ObjectListSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.NotNull(result);
            Assert.IsType<object[]>(result);
            Assert.Empty(nextTasks);

            Assert.Collection((object[])result,
                i => Assert.IsType<Dictionary<string, object>>(i));
        }

        [InlineData(123)]
        [InlineData("abc")]
        [Theory]
        public void ResultIsSingleValue(object input)
        {
            // arrange
            object result = null;
            bool raised = false;
            NamedType namedType = new NamedType("String");

            Action<object> resultIntegratedCallback = r => { result = r; raised = true; };
            IResolveSelectionTask task = CreateSelectionTaskMock(input, resultIntegratedCallback);

            // act
            ObjectListSelectionResultProcessor selectionResultProcessor = new ObjectListSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.NotNull(result);
            Assert.IsType<object[]>(result);
            Assert.Single(nextTasks);

            Assert.Collection((object[])result,
                i => Assert.IsType<Dictionary<string, object>>(i));
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
            ObjectListSelectionResultProcessor selectionResultProcessor = new ObjectListSelectionResultProcessor();
            IResolveSelectionTask[] nextTasks = selectionResultProcessor.Process(task).ToArray();

            // assert
            Assert.True(raised);
            Assert.NotNull(result);
            Assert.IsType<object[]>(result);
            Assert.Equal(3, nextTasks.Length);

            Assert.Collection((object[])result,
                i => Assert.IsType<Dictionary<string, object>>(i),
                i => Assert.IsType<Dictionary<string, object>>(i),
                i => Assert.IsType<Dictionary<string, object>>(i));
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

            Mock<IResolver> resolver = new Mock<IResolver>(MockBehavior.Strict);
            resolver.Setup(t => t.ResolveAsync(It.IsAny<IResolverContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(input);

            Mock<IOptimizedSelection> childSelection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            childSelection.Setup(t => t.CreateContext(It.IsAny<IResolverContext>(), It.IsAny<object>()))
                .Returns(resolverContext.Object);

            Mock<IOptimizedSelection> selection = new Mock<IOptimizedSelection>(MockBehavior.Strict);
            selection.Setup(t => t.TypeDefinition).Returns(default(ObjectTypeDefinition));
            selection.Setup(t => t.FieldDefinition).Returns(default(FieldDefinition));
            selection.Setup(t => t.Name).Returns("foo");
            selection.Setup(t => t.Resolver).Returns(resolver.Object);
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