using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Execution
{
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
