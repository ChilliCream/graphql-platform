using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class RangeTests : TestBase
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
            descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals).Type<DecimalType>();
        }
    }

    public class FloatOperationType : FloatOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.GreaterThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.LowerThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.GreaterThanOrEquals).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.LowerThanOrEquals).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals).Type<DecimalType>();
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
            descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals).Type<DateTimeType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThan).Type<DateTimeType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals).Type<DateTimeType>();
        }
    }

    public class StringOperationType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.GreaterThan).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.LowerThan).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.GreaterThanOrEquals).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.LowerThanOrEquals).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThan).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThan).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals).Type<StringType>();
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
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { value :{ gt: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_NotGreaterThan()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { value :{ ngt: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_GreaterThanOrEquals()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { value :{ gte: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_NotGreaterThanOrEquals()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { value :{ ngte: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_LowerThan()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { value :{ lt: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_NotLowerThan()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { value :{ nlt: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_LowerThanOrEquals()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { value :{ lte: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_NotLowerThanOrEquals()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { value :{ nlte: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Double_LowerThan_And_GreaterThan_Combined()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { and: [{ value :{ lt: 1}} { value :{ gt: -1}}]}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }
    #endregion

    #region Float
    [Fact]
    public async Task ElasticSearch_Range_Float_GreaterThan()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { fValue :{ gt: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_NotGreaterThan()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { fValue :{ ngt: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_GreaterThanOrEquals()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { fValue :{ gte: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_NotGreaterThanOrEquals()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { fValue :{ ngte: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_LowerThan()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { fValue :{ lt: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_NotLowerThan()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { fValue :{ nlt: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_LowerThanOrEquals()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { fValue :{ lte: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_NotLowerThanOrEquals()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { fValue :{ nlte: 0}}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Range_Float_LowerThan_And_GreaterThan_Combined()
    {
        await IndexDocuments(_data);
        IRequestExecutor executorAsync = await CreateExecutorAsync();

        const string query = @"
        {
            test(where: { and: [{ fValue :{ lt: 1}} { fValue :{ gt: -1}}]}) {
                name
                value
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }
    #endregion

    private async Task<IRequestExecutor> CreateExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();
    }

    public class Foo
    {
        public string Name { get; set; } = string.Empty;

        public double Value { get; set; }

        public float FValue { get; set; }

        public DateTime DateTimeValue { get; set; }
    }
}
