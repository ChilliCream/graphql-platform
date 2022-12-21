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
            Value = 1
        },
        new Foo()
        {
            Name = "B",
            Value = 0
        },
        new Foo()
        {
            Name = "C",
            Value = -1
        },
    };



    public RangeTests(ElasticsearchResource resource) : base(resource)
    {
    }

    public class TestOperationType : DecimalOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.GreaterThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.LowerThan).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.GreaterThanOrEquals).Type<DecimalType>();
            descriptor.Operation(DefaultFilterOperations.LowerThanOrEquals).Type<DecimalType>();
        }
    }

    public class FooFilterType : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Value).Type<TestOperationType>();
        }
    }

    [Fact]
    public async Task ElasticSearch_Range_GreaterThan()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

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
    public async Task ElasticSearch_Range_GreaterThanOrEquals()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

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
    public async Task ElasticSearch_Range_LowerThan()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

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
    public async Task ElasticSearch_Range_LowerThanOrEquals()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

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
    public async Task ElasticSearch_Range_LowerThan_And_GreaterThan_Combined()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

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

    public class Foo
    {
        public string Name { get; set; } = string.Empty;

        public double Value { get; set; }
    }
}
