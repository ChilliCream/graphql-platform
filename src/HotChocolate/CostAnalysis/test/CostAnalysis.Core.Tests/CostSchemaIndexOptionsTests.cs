namespace HotChocolate.CostAnalysis;

public class CostSchemaIndexOptionsTests
{
    [Fact]
    public void DefaultListSize_Should_Default_To_PositiveInfinity_When_Constructed()
    {
        // act
        var options = new CostSchemaIndexOptions();

        // assert
        Assert.Equal(double.PositiveInfinity, options.DefaultListSize);
    }

    [Fact]
    public void CaseBudget_Should_Default_To_MeasuredValue()
    {
        // act
        var options = new CostSchemaIndexOptions();

        // assert
        Assert.Equal(510, options.CaseBudget);
    }

    [Fact]
    public void CaseBudgetExceededBehavior_Should_Default_To_EvaluatePerRequest()
    {
        // act
        var options = new CostSchemaIndexOptions();

        // assert
        Assert.Equal(CaseBudgetExceededBehavior.EvaluatePerRequest, options.CaseBudgetExceededBehavior);
    }

    [Theory]
    [InlineData(CaseBudgetExceededBehavior.EvaluatePerRequest)]
    [InlineData(CaseBudgetExceededBehavior.Overestimate)]
    public void CaseBudgetExceededBehavior_Should_RoundTrip_When_Set(CaseBudgetExceededBehavior value)
    {
        // arrange
        var options = new CostSchemaIndexOptions();

        // act
        options.CaseBudgetExceededBehavior = value;

        // assert
        Assert.Equal(value, options.CaseBudgetExceededBehavior);
    }

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void DefaultListSize_Should_ValidateDomain_When_Set(
        double value,
        bool isValid)
    {
        // arrange
        var options = new CostSchemaIndexOptions();

        // act
        var exception = Record.Exception(() => options.DefaultListSize = value);

        // assert
        if (isValid)
        {
            Assert.Null(exception);
            Assert.Equal(value, options.DefaultListSize);
        }
        else
        {
            Assert.IsType<ArgumentOutOfRangeException>(exception);
            Assert.Equal(double.PositiveInfinity, options.DefaultListSize);
        }
    }
}
