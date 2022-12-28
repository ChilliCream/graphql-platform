using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class IntegrationTests : TestBase
{
    private readonly IReadOnlyList<Foo> _data = new[]
    {
        new Foo
        {
            Id = "A",
            Bar = "A",
            Qux = "A",
            Baz = new Baz {Bar = "A", Qux = "A",}
        },
        new Foo
        {
            Id = "B",
            Bar = "B",
            Qux = "B",
            Baz = new Baz {Bar = "B", Qux = "B",}
        },
        new Foo
        {
            Id = "C",
            Bar = "C",
            Qux = "C",
            Baz = new Baz {Bar = "C", Qux = "C",}
        }
    };

    public IntegrationTests(ElasticsearchResource resource) : base(resource)
    {
    }

    public class TestOperationType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotEquals).Type<StringType>();
        }
    }

    public class FooFilterType : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Bar).Type<TestOperationType>();
            descriptor.Field(x => x.Id).Type<TestOperationType>();
            descriptor.Field(x => x.Qux).Type<TestOperationType>();
            descriptor.Field(x => x.Baz).Type<BazFilterType>();
        }
    }

    public class BazFilterType : FilterInputType<Baz>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Baz> descriptor)
        {
            descriptor.Field(x => x.Bar).Type<TestOperationType>();
            descriptor.Field(x => x.Qux).Type<TestOperationType>();
        }
    }

    [Fact]
    public async Task ElasticSearch_SingleField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: { eq: ""A"" }}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_SingleNegatedField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: { neq: ""A"" }}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_MultipleField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {qux: { eq: ""A"" }, bar: { eq: ""A"" }}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_MultipleField_OneNegated()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {qux: { eq: ""A"" }, bar: { neq: ""B"" }}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_AndField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {and: [{bar: { eq: ""B"" }},{qux: { eq: ""B"" }}]}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_AndField_WithNegation()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {and: [{bar: { eq: ""B"" }},{qux: { neq: ""A"" }}]}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_OrField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: {or:[{ eq: ""B"" },{ eq: ""A"" }]}}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_OrField_WithNegation()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: {or:[{ eq: ""B"" },{ neq: ""X"" }]}}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_DeepField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: { baz :{ bar: { eq: ""A"" }}}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_DeepNegatedField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: { baz :{ bar: { neq: ""A"" }}}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    public class Foo
    {
        public string Bar { get; set; } = string.Empty;

        public string Qux { get; set; } = string.Empty;

        public Baz? Baz { get; set; }

        public string Id { get; set; } = string.Empty;
    }

    public class Baz
    {
        public string Bar { get; set; } = string.Empty;

        public string Qux { get; set; } = string.Empty;
    }
}
