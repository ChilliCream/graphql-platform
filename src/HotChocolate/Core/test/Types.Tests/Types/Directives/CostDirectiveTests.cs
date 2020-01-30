using System;
using Xunit;

namespace HotChocolate.Types
{
    public class CostDirectiveTests
    {
        [Fact]
        public void ComplexityEqualsZero()
        {
            // arrange
            // act
            Action a = () => new CostDirective(0);
            Action b = () => new CostDirective(0, "a");

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(a);
            Assert.Throws<ArgumentOutOfRangeException>(b);
        }

        [Fact]
        public void InvalidMultipliers()
        {
            // arrange
            // act
            Action a = () => new CostDirective(1, " ");
            Action b = () => new CostDirective(1, (MultiplierPathString[])null);

            // assert
            Assert.Throws<ArgumentException>(a);
            Assert.Throws<ArgumentNullException>(b);
        }

        [Fact]
        public void ValidMultipliers()
        {
            // arrange
            // act
            var cost = new CostDirective(6, "", "b", "c", null);

            // assert
            Assert.Equal(6, cost.Complexity);
            Assert.Collection(cost.Multipliers,
                s => Assert.Equal("b", s),
                s => Assert.Equal("c", s));
        }

        [Fact]
        public void NoMultipliers()
        {
            // arrange
            // act
            var cost = new CostDirective(5);

            // assert
            Assert.Equal(5, cost.Complexity);
            Assert.Empty(cost.Multipliers);
        }
    }
}
