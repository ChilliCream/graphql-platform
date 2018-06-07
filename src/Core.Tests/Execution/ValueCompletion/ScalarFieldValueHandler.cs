using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Execution
{
    public class ScalarFieldValueHandlerTests
    {
        [Fact]
        public void CompleteStringScalarValue_ShouldSerializeValue()
        {
            // arrange
            string expectedResult = Guid.NewGuid().ToString();
            object result = null;
            bool nextHandlerIsRaised = false;

            StringType stringType = new StringType();

            Mock<IFieldValueCompletionContext> context =
                new Mock<IFieldValueCompletionContext>(MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(stringType);
            context.Setup(t => t.Value).Returns(expectedResult);
            context.Setup(t => t.SetResult(Moq.It.IsAny<string>()))
                .Callback(new Action<object>(v =>
                {
                    result = v;
                }));

            // act
            ScalarFieldValueHandler handler = new ScalarFieldValueHandler();
            handler.CompleteValue(context.Object, c => nextHandlerIsRaised = true);

            // assert
            Assert.Equal(expectedResult, result);
            Assert.False(nextHandlerIsRaised);
        }

        [Fact]
        public void CompleteEnumValueValue_ShouldSerializeValue()
        {
            // arrange
            string expectedResult = "ABC";
            object result = null;
            bool nextHandlerIsRaised = false;

            EnumType enumType = new EnumType(new EnumTypeConfig
            {
                Name = "Foo",
                Values = new List<EnumValueConfig>
                {
                    new EnumValueConfig
                    {
                        Value = "ABC"
                    }
                }
            });

            Mock<IFieldValueCompletionContext> context =
                new Mock<IFieldValueCompletionContext>(MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(enumType);
            context.Setup(t => t.Value).Returns(expectedResult);
            context.Setup(t => t.SetResult(Moq.It.IsAny<string>()))
                .Callback(new Action<object>(v =>
                {
                    result = v;
                }));

            // act
            ScalarFieldValueHandler handler = new ScalarFieldValueHandler();
            handler.CompleteValue(context.Object, c => nextHandlerIsRaised = true);

            // assert
            Assert.Equal(expectedResult, result);
            Assert.False(nextHandlerIsRaised);
        }

        [Fact]
        public void CompleteListOfStringValue_ShouldDelegateToNextHandler()
        {
            // arrange
            string resolverValue = Guid.NewGuid().ToString();
            object result = null;
            bool nextHandlerIsRaised = false;

            ListType listType = new ListType(new StringType());

            Mock<IFieldValueCompletionContext> context =
                new Mock<IFieldValueCompletionContext>(MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(listType);
            context.Setup(t => t.Value).Returns(resolverValue);
            context.Setup(t => t.SetResult(Moq.It.IsAny<string>()))
                .Callback(new Action<object>(v =>
                {
                    result = v;
                }));

            // act
            ScalarFieldValueHandler handler = new ScalarFieldValueHandler();
            handler.CompleteValue(context.Object, c => nextHandlerIsRaised = true);

            // assert
            Assert.Null(result);
            Assert.True(nextHandlerIsRaised);
        }
    }

    public class NonNullFieldValueHandlerTests
    {
        [Fact]
        public void CompleteNonNullStringType_ShouldDelegateWithNewContext()
        {
            // arrange
            bool nextHandlerIsRaised = false;
            bool newContextWasCreated = false;

            NonNullType nonNullType = new NonNullType(new StringType());

            Mock<IFieldValueCompletionContext> context =
                new Mock<IFieldValueCompletionContext>(MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(nonNullType);
            context.Setup(t => t.AsNonNullValueContext())
                .Callback(() => newContextWasCreated = true)
                .Returns(context.Object);

            // act
            NonNullFieldValueHandler handler = new NonNullFieldValueHandler();
            handler.CompleteValue(context.Object, c => nextHandlerIsRaised = true);

            // assert
            Assert.True(nextHandlerIsRaised);
            Assert.True(newContextWasCreated);
        }

        [Fact]
        public void CompleteStringType_ShouldDelegateWithOldContext()
        {
            // arrange
            bool nextHandlerIsRaised = false;
            bool newContextWasCreated = false;

            StringType stringType = new StringType();

            Mock<IFieldValueCompletionContext> context =
                new Mock<IFieldValueCompletionContext>(MockBehavior.Strict);
            context.Setup(t => t.Type).Returns(stringType);
            context.Setup(t => t.AsNonNullValueContext())
                .Callback(() => newContextWasCreated = true)
                .Returns(context.Object);

            // act
            NonNullFieldValueHandler handler = new NonNullFieldValueHandler();
            handler.CompleteValue(context.Object, c => nextHandlerIsRaised = true);

            // assert
            Assert.True(nextHandlerIsRaised);
            Assert.False(newContextWasCreated);
        }
    }
}
