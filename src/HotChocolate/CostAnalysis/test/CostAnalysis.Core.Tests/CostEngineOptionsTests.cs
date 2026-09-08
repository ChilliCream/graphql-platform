namespace HotChocolate.CostAnalysis;

public class CostEngineOptionsTests
{
    [Fact]
    public void DefaultListSize_Should_Default_To_PositiveInfinity_When_Constructed()
    {
        // act
        var options = new CostEngineOptions();

        // assert
        Assert.Equal(double.PositiveInfinity, options.DefaultListSize);
    }

    [Fact]
    public void CaseBudget_Should_Default_To_ProvisionalValue_When_Constructed()
    {
        // act
        var options = new CostEngineOptions();

        // assert
        // 4096 is provisional, the single copy of this number in code,
        // replaced by the core-fix-case-budget-default chore once
        // benchmarks measure a real value.
        Assert.Equal(4096, options.CaseBudget);
    }
}
