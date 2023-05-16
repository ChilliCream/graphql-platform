using HotChocolate.Data.Filters;
using HotChocolate.Types;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class RangeTests : FilterTestBase<RangeTests.Foo, RangeTests.FooFilterType>
{
    private readonly IReadOnlyList<Foo> _data = new[]
    {
        new Foo()
        {
            Name = "A",
            Value = 1,
            FValue = 1,
            DateTimeValue = DateTime.Parse("2022-12-22T15:05:22+0000")
        },
        new Foo()
        {
            Name = "B",
            Value = 0,
            FValue = 0,
            DateTimeValue = DateTime.Parse("2022-12-23T15:05:22+0000")
        },
        new Foo()
        {
            Name = "C",
            Value = -1,
            FValue = -1,
            DateTimeValue = DateTime.Parse("2022-12-24T15:05:22+0000")
        },
    };

    private const string Selection = @"
    name
    value
    fValue
    dateTimeValue
";

    protected override IReadOnlyList<Foo> Data => _data;

    public RangeTests(ElasticsearchResource resource) : base(resource)
    {
    }

    public class DoubleOperationType : DecimalOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.GreaterThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.LowerThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.GreaterThanOrEquals).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.LowerThanOrEquals).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals)
                .Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals).Type<DecimalType>();
        }
    }

    public class FloatOperationType : FloatOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.GreaterThan).Type<FloatType>();
            descriptor.Operation(DefaultFilterOperations.LowerThan).Type<FloatType>();
            descriptor.Operation(DefaultFilterOperations.GreaterThanOrEquals).Type<FloatType>();
            descriptor.Operation(DefaultFilterOperations.LowerThanOrEquals).Type<FloatType>();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThan).Type<FloatType>();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals).Type<FloatType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThan).Type<FloatType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals).Type<FloatType>();
        }
    }

    public class DateTimeOperationType : DateTimeOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.GreaterThan).Type<DateTimeType>();
            descriptor.Operation(DefaultFilterOperations.LowerThan).Type<DateTimeType>();
            descriptor.Operation(DefaultFilterOperations.GreaterThanOrEquals).Type<DateTimeType>();
            descriptor.Operation(DefaultFilterOperations.LowerThanOrEquals).Type<DateTimeType>();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThan).Type<DateTimeType>();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals)
                .Type<DateTimeType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThan).Type<DateTimeType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals).Type<DateTimeType>();
        }
    }

    public class FooFilterType : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Value).Type<DoubleOperationType>();
            descriptor.Field(x => x.FValue).Type<FloatOperationType>();
            descriptor.Field(x => x.DateTimeValue).Type<DateTimeOperationType>();
        }
    }

    #region Double

    [Fact]
    public async Task ElasticSearch_Range_Double_GreaterThan()
    {
        var result = await ExecuteFilterTest(@"value :{ gt: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_NotGreaterThan()
    {
        var result = await ExecuteFilterTest(@"value :{ ngt: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_GreaterThanOrEquals()
    {
        var result = await ExecuteFilterTest(@"value :{ gte: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_NotGreaterThanOrEquals()
    {
        var result = await ExecuteFilterTest(@"value :{ ngte: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_LowerThan()
    {
        var result = await ExecuteFilterTest(@"value :{ lt: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_NotLowerThan()
    {
        var result = await ExecuteFilterTest(@"value :{ nlt: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_LowerThanOrEquals()
    {
        var result = await ExecuteFilterTest(@"value :{ lte: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_NotLowerThanOrEquals()
    {
        var result = await ExecuteFilterTest(@"value :{ nlte: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_LowerThan_And_GreaterThan_Combined()
    {
        var result =
            await ExecuteFilterTest(@"and: [{ value :{ lt: 1}} { value :{ gt: -1}}]", Selection);
        result.MatchQuerySnapshot();
    }

    #endregion

    #region Float

    [Fact]
    public async Task ElasticSearch_Range_Float_GreaterThan()
    {
        var result = await ExecuteFilterTest(@"fValue :{ gt: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_NotGreaterThan()
    {
        var result = await ExecuteFilterTest(@"fValue :{ ngt: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_GreaterThanOrEquals()
    {
        var result = await ExecuteFilterTest(@"fValue :{ gte: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_NotGreaterThanOrEquals()
    {
        var result = await ExecuteFilterTest(@"fValue :{ ngte: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_LowerThan()
    {
        var result = await ExecuteFilterTest(@"fValue :{ lt: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_NotLowerThan()
    {
        var result = await ExecuteFilterTest(@"fValue :{ nlt: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_LowerThanOrEquals()
    {
        var result = await ExecuteFilterTest(@"fValue :{ lte: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_NotLowerThanOrEquals()
    {
        var result = await ExecuteFilterTest(@"fValue :{ nlte: 0}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_LowerThan_And_GreaterThan_Combined()
    {
        var result = await ExecuteFilterTest(@"and: [{ fValue :{ lt: 1}} { fValue :{ gt: -1}}]",
            Selection);
        result.MatchQuerySnapshot();
    }

    #endregion

    #region DateTime

    [Fact]
    public async Task ElasticSearch_Range_DateTime_GreaterThan()
    {
        var result = await ExecuteFilterTest(@"dateTimeValue :{ gt: ""2022-12-23T15:05:22+0000""}",
            Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_DateTime_NotGreaterThan()
    {
        var result = await ExecuteFilterTest(@"dateTimeValue :{ ngt: ""2022-12-23T15:05:22+0000""}",
            Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_DateTime_GreaterThanOrEquals()
    {
        var result = await ExecuteFilterTest(@"dateTimeValue :{ gte: ""2022-12-23T15:05:22+0000""}",
            Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_DateTime_NotGreaterThanOrEquals()
    {
        var result =
            await ExecuteFilterTest(@"dateTimeValue :{ ngte: ""2022-12-23T15:05:22+0000""}",
                Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_DateTime_LowerThan()
    {
        var result = await ExecuteFilterTest(@"dateTimeValue :{ lt: ""2022-12-23T15:05:22+0000""}",
            Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_DateTime_NotLowerThan()
    {
        var result = await ExecuteFilterTest(@"dateTimeValue :{ nlt: ""2022-12-23T15:05:22+0000""}",
            Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_DateTime_LowerThanOrEquals()
    {
        var result = await ExecuteFilterTest(@"dateTimeValue :{ lte: ""2022-12-23T15:05:22+0000""}",
            Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_DateTime_NotLowerThanOrEquals()
    {
        var result =
            await ExecuteFilterTest(@"dateTimeValue :{ nlte: ""2022-12-23T15:05:22+0000""}",
                Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_DateTime_LowerThan_And_GreaterThan_Combined()
    {
        var result = await ExecuteFilterTest(
            @"and: [{ dateTimeValue :{ gt: ""2022-12-22T15:05:22+0000""}} { dateTimeValue :{ lt: ""2022-12-24T15:05:22+0000""}}]",
            Selection);
        result.MatchQuerySnapshot();
    }

    #endregion

    public class Foo
    {
        public string Name { get; set; } = string.Empty;

        public double Value { get; set; }

        public float FValue { get; set; }

        public DateTime DateTimeValue { get; set; }
    }

    /// <inheritdoc />
}
