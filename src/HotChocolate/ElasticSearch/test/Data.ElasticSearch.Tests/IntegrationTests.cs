using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

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
            Baz = new Baz {Bar = "A", Qux = "A"}
        },
        new Foo
        {
            Id = "B",
            Bar = "B",
            Qux = "B",
            Baz = new Baz {Bar = "B", Qux = "B"}
        },
        new Foo
        {
            Id = "C",
            Bar = "C",
            Qux = "C",
            Baz = new Baz {Bar = "C", Qux = "C"}
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
    public async Task Execute_Should_MatchSnapshot_When_SingleField()
    {
        await IndexDocuments(_data);

        var executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = """

        {
            test(where: {bar: { eq: "A" }}) {
                bar
            }
        }
        
""";

        var result = await executorAsync.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_SingleNegatedField()
    {
        await IndexDocuments(_data);

        var executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = """

        {
            test(where: {bar: { neq: "A" }}) {
                bar
            }
        }
        
""";

        var result = await executorAsync.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_MultipleField()
    {
        await IndexDocuments(_data);

        var executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = """

        {
            test(where: {qux: { eq: "A" }, bar: { eq: "A" }}) {
                bar
            }
        }
        
""";

        var result = await executorAsync.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_MultipleField_OneNegated()
    {
        await IndexDocuments(_data);

        var executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = """

        {
            test(where: {qux: { eq: "A" }, bar: { neq: "B" }}) {
                bar
            }
        }
        
""";

        var result = await executorAsync.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_AndField()
    {
        await IndexDocuments(_data);

        var executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = """

        {
            test(where: {and: [{bar: { eq: "B" }},{qux: { eq: "B" }}]}) {
                bar
            }
        }
        
""";

        var result = await executorAsync.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_AndField_WithNegation()
    {
        await IndexDocuments(_data);

        var executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = """

        {
            test(where: {and: [{bar: { eq: "B" }},{qux: { neq: "A" }}]}) {
                bar
            }
        }
        
""";

        var result = await executorAsync.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_OrField()
    {
        await IndexDocuments(_data);

        var executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = """

        {
            test(where: {bar: {or:[{ eq: "B" },{ eq: "A" }]}}) {
                bar
            }
        }
        
""";

        var result = await executorAsync.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_OrField_WithNegation()
    {
        await IndexDocuments(_data);

        var executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = """

        {
            test(where: {bar: {or:[{ eq: "B" },{ neq: "X" }]}}) {
                bar
            }
        }
        
""";

        var result = await executorAsync.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_DeepField()
    {
        await IndexDocuments(_data);

        var executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = """

        {
            test(where: { baz :{ bar: { eq: "A" }}}) {
                bar
            }
        }
        
""";

        var result = await executorAsync.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_DeepNegatedField()
    {
        await IndexDocuments(_data);

        var executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = """

        {
            test(where: { baz :{ bar: { neq: "A" }}}) {
                bar
            }
        }
        
""";

        var result = await executorAsync.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_FilterWithDefaultProvider_When_SourceIsNotElasticSearch()
    {
        // arrange
        Foo[] data = [.. _data, new Foo { Id = "D", Bar = "D", Qux = "D" }];

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x =>
            {
                x.Name("Query");
                x.Field("list").UseFiltering<FooFilterType>().Resolve<List<Foo>>(_ => [.. data]);
                x.Field("queryable").UseFiltering<FooFilterType>().Resolve(_ => data.AsQueryable());
            })
            .AddElasticSearchFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                list(where: { baz: { bar: { eq: "A" } } }) { bar }
                queryable(where: { baz: { bar: { eq: "A" } } }) { bar }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var json = JsonNode.Parse(result.ToJson())!;
        Assert.Null(json["errors"]);
        Assert.Equal("[{\"bar\":\"A\"}]", json["data"]!["list"]!.ToJsonString());
        Assert.Equal("[{\"bar\":\"A\"}]", json["data"]!["queryable"]!.ToJsonString());
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
