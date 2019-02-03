using System;
using Xunit;

namespace HotChocolate
{
    public class ErrorBuilderTests
    {
        [Fact]
        public void FromError()
        {
            // arrange
            IError error = new Error { Message = "123" };

            // act
            ErrorBuilder builder = ErrorBuilder.FromError(error);
            error = builder.Build();

            // assert
            Assert.Equal("123", error.Message);
        }

        [Fact]
        public void FromError_ErrorNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => ErrorBuilder.FromError(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
