using System;
using HotChocolate.AspNetCore.RateLimit;
using Xunit;

namespace HotChocolate.RateLimit.Tests
{
    public class LimitOptionsTests
    {
        [Fact]
        public void GivenLimitOptions_WhenAddPolicy_ShouldHavePolicy()
        {
            // Arrange
            var limitOptions = new LimitOptions();

            // Act
            limitOptions.AddPolicy("policy", pb => pb.WithLimit(TimeSpan.FromMinutes(1), 1));

            // Assert
            Assert.Contains(limitOptions.Policies, pair => pair.Key == "policy");
        }

        [Fact]
        public void GivenLimitOptions_WhenAddPolicyWithEmptyName_ShouldThrow()
        {
            // Arrange
            var limitOptions = new LimitOptions();

            // Act
            // Assert
            Assert.Throws<ArgumentException>(() =>
                limitOptions.AddPolicy(string.Empty, pb => { }));
        }

        [Fact]
        public void GivenLimitOptions_WhenAddPolicyWithNullConfiguration_ShouldThrow()
        {
            // Arrange
            var limitOptions = new LimitOptions();

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                limitOptions.AddPolicy("policy", null!));
        }
    }
}
