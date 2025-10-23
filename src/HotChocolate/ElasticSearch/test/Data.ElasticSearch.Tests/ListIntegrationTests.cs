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
public class ListIntegrationTests : TestBase
{
    private readonly IReadOnlyList<Foo> _data = new[]
    {
        new Foo
        {
            Bar = "A",
            ScalarList = {"A1", "A2", "A3"},
            ObjectList =
            {
                new Baz {Bar = "A1", Qux = "A1"},
                new Baz {Bar = "A2", Qux = "A2"},
                new Baz {Bar = "A3", Qux = "A3"}
            }
        },
        new Foo
        {
            Bar = "B",
            ScalarList = {"B1", "B2", "B3"},
            ObjectList =
            {
                new Baz {Bar = "B1", Qux = "B1"},
                new Baz {Bar = "B2", Qux = "B2"},
                new Baz {Bar = "B3", Qux = "B3"}
            }
        },
        new Foo
        {
            Bar = "C",
            ScalarList = {"C1", "C2", "C3"},
            ObjectList =
            {
                new Baz {Bar = "C1", Qux = "C1"},
                new Baz {Bar = "C2", Qux = "C2"},
                new Baz {Bar = "C3", Qux = "C3"}
            }
        },
        new Foo {Bar = "A"},
    };

    public ListIntegrationTests(ElasticsearchResource resource) : base(resource)
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
            descriptor.Field(x => x.ScalarList).Type<ArrayFilterInputType<TestOperationType>>();
        }
    }

    [Fact]
    public async Task ElasticSearch_Scalar_Some()
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
            test(where: {scalarList: { some: { eq: ""A1"" }}}) {
                bar
                scalarList
                objectList {
                    bar qux
                }
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Scalar_Some_Negated()
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
            test(where: {scalarList: { some: { neq: ""A1"" }}}) {
                bar
                scalarList
                objectList {
                    bar qux
                }
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Scalar_None()
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
            test(where: {scalarList: { none: { eq: ""A1"" }}}) {
                bar
                scalarList
                objectList {
                    bar qux
                }
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Scalar_None_Negate()
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
            test(where: {scalarList: { none: { neq: ""A1"" }}}) {
                bar
                scalarList
                objectList {
                    bar qux
                }
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Scalar_Any_True()
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
            test(where: {scalarList: { any: true}}) {
                bar
                scalarList
                objectList {
                    bar qux
                }
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Scalar_Any_False()
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
            test(where: {scalarList: { any: false}}) {
                bar
                scalarList
                objectList {
                    bar qux
                }
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    public class Foo
    {
        public string Bar { get; set; } = string.Empty;

        public List<string> ScalarList { get; set; } = new();

        public List<Baz> ObjectList { get; set; } = new();
    }

    public class Baz
    {
        public string Bar { get; set; } = string.Empty;

        public string Qux { get; set; } = string.Empty;
    }
}
