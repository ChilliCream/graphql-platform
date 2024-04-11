using System;

namespace HotChocolate.Types.Directives;

public class CostDirectiveTests
{
    [Fact]
    public void ComplexitySmallerThanZero()
    {
        // arrange
        // act
        void Action() => new CostDirective(-1);
        void Action1() => new CostDirective(-1, "a");

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Action);
        Assert.Throws<ArgumentOutOfRangeException>(Action1);
    }

    [Fact]
    public void InvalidMultipliers()
    {
        // arrange
        // act
        void Action() => new CostDirective(1, " ");
        void Action1() => new CostDirective(1, null);

        // assert
        Assert.Throws<ArgumentException>(Action);
        Assert.Throws<ArgumentNullException>(Action1);
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
