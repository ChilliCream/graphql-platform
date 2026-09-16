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
