using System.Reflection;

namespace HotChocolate.CostAnalysis;

public sealed class OptionsTests
{
    [Fact]
    public void CostOptions_Should_HaveExpectedCostDefaults_When_Constructed()
    {
        // arrange & act
        var options = new CostOptions();

        // assert
        Assert.Equal(1_000, options.MaxFieldCost);
        Assert.Equal(1_000, options.MaxTypeCost);
        Assert.Equal(double.PositiveInfinity, options.DefaultListSize);
    }

    [Fact]
    public void CostOptions_Should_HaveExpectedEngineDefaults_When_Constructed()
    {
        // arrange & act
        var options = new CostOptions();

        // assert
        Assert.Equal(256, options.CostPlanCacheSize);
        Assert.Null(options.MaxResponseSize);
        Assert.Null(options.CaseBudget);
    }

    [Theory]
    [InlineData(typeof(FilterCostOptions), "VariableMultiplier")]
    [InlineData(typeof(SortCostOptions), "VariableMultiplier")]
    [InlineData(typeof(RequestCostOptions), "FilterVariableMultiplier")]
    public void VariableMultiplier_Should_BeObsoleteError_When_Reflected(Type declaringType, string memberName)
    {
        // arrange
        var property = declaringType.GetProperty(memberName);

        // act
        var obsolete = property?.GetCustomAttribute<ObsoleteAttribute>();

        // assert
        Assert.NotNull(obsolete);
        Assert.True(obsolete.IsError);
    }
}
