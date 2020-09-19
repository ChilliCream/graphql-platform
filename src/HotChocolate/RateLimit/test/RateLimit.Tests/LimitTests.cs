using System;
using HotChocolate.AspNetCore.RateLimit;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.RateLimit.Tests
{
    public class LimitTests
    {
        [Fact]
        public void GivenValidLimit_WhenIsValid_ShouldBeTrue()
        {
            // Arrange
            var limit = Limit.Create(DateTime.UtcNow.AddMinutes(-1), 1);
            var policyIdentifiers = new IPolicyIdentifier[] { new ClaimsPolicyIdentifier("sub") };
            var limitPolicy = new LimitPolicy(policyIdentifiers, TimeSpan.FromMinutes(2), 1);

            // Act
            var result = limit.IsValid(limitPolicy);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GivenExpiredLimit_WhenIsExpired_ShouldBeTrue()
        {
            // Arrange
            var limit = Limit.Create(DateTime.UtcNow.AddMinutes(-1), 1);
            var policyIdentifiers = new IPolicyIdentifier[] { new ClaimsPolicyIdentifier("sub") };
            var limitPolicy = new LimitPolicy(policyIdentifiers, TimeSpan.FromSeconds(10), 1);

            // Act
            var result = limit.IsExpired(limitPolicy);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GivenValidLimit_WhenIsExpired_ShouldBeFalse()
        {
            // Arrange
            var limit = Limit.Create(DateTime.UtcNow.AddMinutes(-1), 1);
            var policyIdentifiers = new IPolicyIdentifier[] { new ClaimsPolicyIdentifier("sub") };
            var limitPolicy = new LimitPolicy(policyIdentifiers, TimeSpan.FromMinutes(2), 1);

            // Act
            var result = limit.IsExpired(limitPolicy);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GivenInvalidLimit_WhenIsValid_ShouldBeFalse()
        {
            // Arrange
            var limit = Limit.Create(DateTime.UtcNow.AddMinutes(-1), 2);
            var policyIdentifiers = new IPolicyIdentifier[] { new ClaimsPolicyIdentifier("sub") };
            var limitPolicy = new LimitPolicy(policyIdentifiers, TimeSpan.FromSeconds(10), 1);

            // Act
            var result = limit.IsValid(limitPolicy);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GivenLimit_WhenConvertToByteAndBack_ShouldBeTheSame()
        {
            // Arrange
            var limit = Limit.Create(DateTime.MaxValue, int.MaxValue);

            // Act
            byte[] payload = limit.ToByte();

            // Assert
            payload.ToLimit().MatchSnapshot();
        }
    }
}
