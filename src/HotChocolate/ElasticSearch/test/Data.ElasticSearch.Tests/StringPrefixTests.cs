using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class StringPrefixTests : TestBase
{
    private readonly IReadOnlyList<Foo> _data = new[]
    {
        new Foo
        {
            Id = "A",
            Bar = "this starts with this",
            Qux = "does starts with something",
            Baz = new Baz {Bar = "does start with another thing", Qux = "don't care"}
        },
        new Foo
        {
            Id = "B",
            Bar = "that starts with that",
            Qux = "does not starts with anything :)",
            Baz = new Baz {Bar = "does starts with something", Qux = "don't care"}
        },
        new Foo
        {
            Id = "C",
            Bar = "I start with nothing",
            Qux = "you may start with something",
            Baz = new Baz
            {
                Bar = "he can start with anything",
                Qux = "we start with passion!"
            }
        }
    };

    public StringPrefixTests(ElasticsearchResource resource) : base(resource)
    {
    }

    public class TestOperationType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.StartsWith).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotStartsWith).Type<StringType>();
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
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: { startsWith: ""th"" }}) {
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
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: { nstartsWith: ""th"" }}) {
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
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {qux: { startsWith: ""do"" }, bar: { startsWith: ""th"" }}) {
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
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {qux: { startsWith: ""do"" }, bar: { nstartsWith: ""th"" }}) {
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
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {
                  and: [
                    {bar: { startsWith: ""th"" }},
                    {qux: { startsWith: ""do"" }}
                    ]}) {
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
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {
                    and: [
                        {bar: { startsWith: ""th"" }},
                        {qux: { nstartsWith: ""do"" }}
                ]}) {
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
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: {or:[{ startsWith: ""th"" },{ startsWith: ""I"" }]}}) {
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
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: {or:[{ startsWith: ""th"" },{ nstartsWith: ""I"" }]}}) {
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
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: { baz :{ bar: { startsWith: ""he"" }}}) {
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
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: { baz :{ bar: { nstartsWith: ""he"" }}}) {
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
