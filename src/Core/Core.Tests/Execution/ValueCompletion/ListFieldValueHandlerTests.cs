using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Execution
{
    public class ListFieldValueHandlerTests
    {
        [Fact]
        public void CompleteListOfStrings_ShouldSerializeValue()
        {
            // arrange
            string expectedElement = Guid.NewGuid().ToString();
            List<string> list = new List<string> { expectedElement };

            object result = null;
            object element = null;
            bool nextHandlerIsRaised = false;
            int elements = 0;

            ListType listType = new ListType(new StringType());

            Mock<IFieldValueCompletionContext> context =
                new Mock<IFieldValueCompletionContext>(MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(listType);
            context.Setup(t => t.Value).Returns(list);
            context.Setup(t => t.Path).Returns(Path.New("root"));
            context.Setup(t => t.IntegrateResult(Moq.It.IsAny<List<object>>()))
                .Callback(new Action<object>(v =>
                {
                    result = v;
                }));
            context.Setup(t => t.AsElementValueContext(
                Moq.It.IsAny<Path>(), Moq.It.IsAny<IType>(),
                Moq.It.IsAny<object>(), Moq.It.IsAny<Action<object>>()))
                .Callback(new Action<Path, IType, object, Action<object>>(
                    (a, b, value, c) =>
                    {
                        elements++;
                        element = value;
                    }))
                .Returns(context.Object);

            // act
            ListFieldValueHandler handler = new ListFieldValueHandler();
            handler.CompleteValue(context.Object, c => nextHandlerIsRaised = true);

            // assert
            Assert.IsType<List<object>>(result);
            Assert.Empty((List<object>)result);
            Assert.Equal(1, elements);
            Assert.True(nextHandlerIsRaised);
        }

        [Fact]
        public void CompleteListOfNonNullStrings_ShouldSerializeValue()
        {
            // arrange
            string expectedElement = Guid.NewGuid().ToString();
            List<string> list = new List<string> { expectedElement };

            object result = null;
            object element = null;
            bool nextHandlerIsRaised = false;
            int elements = 0;

            ListType listType = new ListType(new NonNullType(new StringType()));

            Mock<IFieldValueCompletionContext> context =
                new Mock<IFieldValueCompletionContext>(MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(listType);
            context.Setup(t => t.Value).Returns(list);
            context.Setup(t => t.Path).Returns(Path.New("root"));
            context.Setup(t => t.IntegrateResult(Moq.It.IsAny<List<object>>()))
                .Callback(new Action<object>(v =>
                {
                    result = v;
                }));
            context.Setup(t => t.AsElementValueContext(
                Moq.It.IsAny<Path>(), Moq.It.IsAny<IType>(),
                Moq.It.IsAny<object>(), Moq.It.IsAny<Action<object>>()))
                .Callback(new Action<Path, IType, object, Action<object>>(
                    (a, b, value, c) =>
                    {
                        elements++;
                        element = value;
                    }))
                .Returns(context.Object);

            // act
            ListFieldValueHandler handler = new ListFieldValueHandler();
            handler.CompleteValue(context.Object, c => nextHandlerIsRaised = true);

            // assert
            Assert.IsType<List<object>>(result);
            Assert.Empty((List<object>)result);
            Assert.Equal(1, elements);
            Assert.True(nextHandlerIsRaised);
        }

        [Fact]
        public void CompleteListOfNonNullStrings_WithNullElement_ShouldThrowError()
        {
            // arrange
            List<string> list = new List<string> { null };

            object result = null;
            object element = null;
            bool nextHandlerIsRaised = false;
            bool errorWasRaised = false;
            int elements = 0;

            ListType listType = new ListType(new NonNullType(new StringType()));

            Mock<IFieldValueCompletionContext> context =
                new Mock<IFieldValueCompletionContext>(MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(listType);
            context.Setup(t => t.Value).Returns(list);
            context.Setup(t => t.Path).Returns(Path.New("root"));
            context.Setup(t => t.IntegrateResult(Moq.It.IsAny<List<object>>()))
                .Callback(new Action<object>(v =>
                {
                    result = v;
                }));
            context.Setup(t => t.AsElementValueContext(
                Moq.It.IsAny<Path>(), Moq.It.IsAny<IType>(),
                Moq.It.IsAny<object>(), Moq.It.IsAny<Action<object>>()))
                .Callback(new Action<Path, IType, object, Action<object>>(
                    (a, b, value, c) =>
                    {
                        elements++;
                        element = value;
                    }))
                .Returns(context.Object);
            context.Setup(t => t.ReportError(Moq.It.IsAny<string>()))
                .Callback(() => errorWasRaised = true);

            // act
            ListFieldValueHandler handler = new ListFieldValueHandler();
            handler.CompleteValue(context.Object, c => nextHandlerIsRaised = true);

            // assert
            Assert.Null(result);
            Assert.Equal(0, elements);
            Assert.False(nextHandlerIsRaised);
            Assert.True(errorWasRaised);
        }

        [Fact]
        public void CompleteStringType_ShouldCallNextHandler()
        {
            // arrange
            bool nextHandlerIsRaised = false;

            StringType stringType = new StringType();

            Mock<IFieldValueCompletionContext> context =
                new Mock<IFieldValueCompletionContext>(MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(stringType);

            // act
            ListFieldValueHandler handler = new ListFieldValueHandler();
            handler.CompleteValue(context.Object, c => nextHandlerIsRaised = true);

            // assert
            Assert.True(nextHandlerIsRaised);
        }

        [Fact]
        public void CompleteListOfStrings_ValueDoesNotImplementIEnumerable_ShouldThrowError()
        {
            // arrange
            bool errorWasRaised = false;
            bool nextHandlerIsRaised = false;

            ListType listType = new ListType(new StringType());

            Mock<IFieldValueCompletionContext> context =
                new Mock<IFieldValueCompletionContext>(MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(listType);
            context.Setup(t => t.Value).Returns(1);
            context.Setup(t => t.Path).Returns(Path.New("root"));
            context.Setup(t => t.ReportError(Moq.It.IsAny<string>()))
                .Callback(() => errorWasRaised = true);

            // act
            ListFieldValueHandler handler = new ListFieldValueHandler();
            handler.CompleteValue(context.Object, c => nextHandlerIsRaised = true);

            // assert
            Assert.True(errorWasRaised);
            Assert.False(nextHandlerIsRaised);
        }

        [Fact]
        public void CompleteList_WithNullValues_ShouldNotCompleteWithNext()
        {
            // arrange
            string expectedElement = Guid.NewGuid().ToString();
            var list = new List<string> { null };

            object result = null;
            bool nextHandlerIsRaised = false;

            ListType listType = new ListType(new StringType());

            var context = new Mock<IFieldValueCompletionContext>(
                MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(listType);
            context.Setup(t => t.Value).Returns(list);
            context.Setup(t => t.Path).Returns(Path.New("root"));
            context.Setup(t => t.IntegrateResult(Moq.It.IsAny<List<object>>()))
                .Callback(new Action<object>(v =>
                {
                    result = v;
                }));
            context.Setup(t => t.AsElementValueContext(
                Moq.It.IsAny<Path>(), Moq.It.IsAny<IType>(),
                Moq.It.IsAny<object>(), Moq.It.IsAny<Action<object>>()))
                .Returns(context.Object);

            // act
            var handler = new ListFieldValueHandler();
            handler.CompleteValue(
                context.Object,
                c => nextHandlerIsRaised = true);

            // assert
            Assert.IsType<List<object>>(result);
            Assert.NotEmpty((List<object>)result);
            Assert.False(nextHandlerIsRaised);
        }
    }
}
