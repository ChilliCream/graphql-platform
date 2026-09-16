using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

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
    public void CostOptions_Should_HaveExpectedDefaults_When_Constructed()
    {
        // arrange & act
        var options = new CostOptions();

        // assert
        Assert.Equal(256, options.CostPlanCacheSize);
        Assert.Null(options.MaxResponseSize);
        Assert.Null(options.CaseBudget);
    }

    [Theory]
    [InlineData(null, 510)]
    [InlineData(16, 16)]
    public async Task AddCostAnalyzer_Should_CreateImmutableSchemaIndex_When_CaseBudgetIsConfigured(
        int? caseBudget,
        int expectedCaseBudget)
    {
        // arrange
        var requestExecutor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("hello").Resolve("world"))
            .ModifyCostOptions(o => o.CaseBudget = caseBudget)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var schemaIndex = requestExecutor.Schema.Services.GetRequiredService<CostSchemaIndex>();
        var optionsCopy = schemaIndex.Options;
        optionsCopy.CaseBudget = -1;

        // assert
        Assert.Same(
            schemaIndex,
            requestExecutor.Schema.Services.GetRequiredService<CostSchemaIndex>());
        Assert.Equal(expectedCaseBudget, schemaIndex.Options.CaseBudget);
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

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void MaxFieldCost_Should_ValidateDomain_When_SetOnCostOptions(
        double value,
        bool isValid)
    {
        // arrange
        var options = new CostOptions();

        // act
        var exception = Record.Exception(() => options.MaxFieldCost = value);

        // assert
        if (isValid)
        {
            Assert.Null(exception);
            Assert.Equal(value, options.MaxFieldCost);
        }
        else
        {
            Assert.IsType<ArgumentOutOfRangeException>(exception);
            Assert.Equal(1_000, options.MaxFieldCost);
        }
    }

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void MaxTypeCost_Should_ValidateDomain_When_SetOnCostOptions(
        double value,
        bool isValid)
    {
        // arrange
        var options = new CostOptions();

        // act
        var exception = Record.Exception(() => options.MaxTypeCost = value);

        // assert
        if (isValid)
        {
            Assert.Null(exception);
            Assert.Equal(value, options.MaxTypeCost);
        }
        else
        {
            Assert.IsType<ArgumentOutOfRangeException>(exception);
            Assert.Equal(1_000, options.MaxTypeCost);
        }
    }

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void DefaultListSize_Should_ValidateDomain_When_SetOnCostOptions(
        double value,
        bool isValid)
    {
        // arrange
        var options = new CostOptions();

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

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(null, true)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void MaxResponseSize_Should_ValidateDomain_When_SetOnCostOptions(
        double? value,
        bool isValid)
    {
        // arrange
        var options = new CostOptions();

        // act
        var exception = Record.Exception(() => options.MaxResponseSize = value);

        // assert
        if (isValid)
        {
            Assert.Null(exception);
            Assert.Equal(value, options.MaxResponseSize);
        }
        else
        {
            Assert.IsType<ArgumentOutOfRangeException>(exception);
            Assert.Null(options.MaxResponseSize);
        }
    }

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void MaxFieldCost_Should_ValidateDomain_When_OverriddenPerRequest(
        double value,
        bool isValid)
    {
        // arrange
        var options = new RequestCostOptions(10, 10, true, false, (double?)null);
        RequestCostOptions? constructed = null;
        RequestCostOptions? modified = null;

        // act
        var constructionException = Record.Exception(
            () => constructed = new RequestCostOptions(value, 10, true, false, (double?)null));
        var withException = Record.Exception(
            () => modified = options with { MaxFieldCost = value });

        // assert
        if (isValid)
        {
            Assert.Null(constructionException);
            Assert.Null(withException);
            Assert.Equal(value, constructed!.MaxFieldCost);
            Assert.Equal(value, modified!.MaxFieldCost);
        }
        else
        {
            Assert.IsType<ArgumentOutOfRangeException>(constructionException);
            Assert.IsType<ArgumentOutOfRangeException>(withException);
        }
    }

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void MaxTypeCost_Should_ValidateDomain_When_OverriddenPerRequest(
        double value,
        bool isValid)
    {
        // arrange
        var options = new RequestCostOptions(10, 10, true, false, (double?)null);
        RequestCostOptions? constructed = null;
        RequestCostOptions? modified = null;

        // act
        var constructionException = Record.Exception(
            () => constructed = new RequestCostOptions(10, value, true, false, (double?)null));
        var withException = Record.Exception(
            () => modified = options with { MaxTypeCost = value });

        // assert
        if (isValid)
        {
            Assert.Null(constructionException);
            Assert.Null(withException);
            Assert.Equal(value, constructed!.MaxTypeCost);
            Assert.Equal(value, modified!.MaxTypeCost);
        }
        else
        {
            Assert.IsType<ArgumentOutOfRangeException>(constructionException);
            Assert.IsType<ArgumentOutOfRangeException>(withException);
        }
    }

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(null, true)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void MaxResponseSize_Should_ValidateDomain_When_OverriddenPerRequest(
        double? value,
        bool isValid)
    {
        // arrange
        var options = new RequestCostOptions(10, 10, true, false, (double?)null);
        RequestCostOptions? constructed = null;
        RequestCostOptions? modified = null;

        // act
        var constructionException = Record.Exception(
            () => constructed = new RequestCostOptions(10, 10, true, false, value));
        var withException = Record.Exception(
            () => modified = options with { MaxResponseSize = value });

        // assert
        if (isValid)
        {
            Assert.Null(constructionException);
            Assert.Null(withException);
            Assert.Equal(value, constructed!.MaxResponseSize);
            Assert.Equal(value, modified!.MaxResponseSize);
        }
        else
        {
            Assert.IsType<ArgumentOutOfRangeException>(constructionException);
            Assert.IsType<ArgumentOutOfRangeException>(withException);
        }
    }
}
