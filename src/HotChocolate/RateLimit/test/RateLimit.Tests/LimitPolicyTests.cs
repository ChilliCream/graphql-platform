using System;
using HotChocolate.AspNetCore.RateLimit;
using Xunit;

namespace HotChocolate.RateLimit.Tests
{
    public class LimitPolicyTests
    {
        [Fact]
        public void WhenCreateLimitPolicy_ShouldBeValid()
        {
            // Arrange
            var policyIdentifiers = new IPolicyIdentifier[] { new ClaimsPolicyIdentifier("sub") };

            // Act
            var limitPolicy = new LimitPolicy(policyIdentifiers, TimeSpan.FromMinutes(2), 1);

            // Assert
            Assert.NotNull(limitPolicy);
        }

        [Fact]
        public void WhenCreateLimitPolicy_ShouldThrowIdentifiersException()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LimitPolicy(null!, TimeSpan.FromSeconds(1), 1));
        }

        [Fact]
        public void WhenCreateLimitPolicy_ShouldThrowPeriodException()
        {
            // Arrange
            var policyIdentifiers = new IPolicyIdentifier[] { new ClaimsPolicyIdentifier("sub") };

            // Act
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LimitPolicy(policyIdentifiers, TimeSpan.FromSeconds(0.9), 1));
        }

        [Fact]
        public void WhenCreateLimitPolicy_ShouldThrowLimitException()
        {
            // Arrange
            var policyIdentifiers = new IPolicyIdentifier[] { new ClaimsPolicyIdentifier("sub") };

            // Act
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LimitPolicy(policyIdentifiers, TimeSpan.FromMinutes(2), 0));
        }
    }
}
