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
    public void CaseBudget_Should_Default_To_MeasuredValue()
    {
        // act
        var options = new CostEngineOptions();

        // assert
        Assert.Equal(510, options.CaseBudget);
    }
}
