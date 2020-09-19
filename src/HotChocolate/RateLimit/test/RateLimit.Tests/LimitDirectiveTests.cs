using System;
using Xunit;

namespace HotChocolate.RateLimit.Tests
{
    public class LimitDirectiveTests
    {
        [Fact]
        public void WhenCreateLimitDirective_ShouldBeValid()
        {
            // Arrange
            // Act
            var limitDirective = new LimitDirective("policy");

            // Assert
            Assert.NotNull(limitDirective);
        }

        [Fact]
        public void WhenCreateLimitDirective_ShouldThrow()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentException>(() => new LimitDirective(string.Empty));
        }
    }
}
