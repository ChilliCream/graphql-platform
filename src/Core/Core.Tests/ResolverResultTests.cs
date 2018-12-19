using System;
using Xunit;

namespace HotChocolate
{
    public class ResolverResultTests
    {
        [Fact]
        public void CreateValue()
        {
            // arrange
            string value = Guid.NewGuid().ToString();

            // act
            var result = ResolverResult<string>.CreateValue(value);

            // assert
            Assert.Equal(value, result.Value);
            Assert.False(result.IsError);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void CreateError()
        {
            // arrange
            string errorMessage = Guid.NewGuid().ToString();

            // act
            var result = ResolverResult<string>.CreateError(errorMessage);

            // assert
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.True(result.IsError);
            Assert.Null(result.Value);
        }
    }
}
