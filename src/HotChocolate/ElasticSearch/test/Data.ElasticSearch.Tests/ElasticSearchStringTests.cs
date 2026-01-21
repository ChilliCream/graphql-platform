using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class ElasticSearchStringTests : TestBase
{
    private readonly IReadOnlyList<Foo> _data = new[]
    {
        new Foo {Id = "A", Bar = "A"},
        new Foo {Id = "B", Bar = "B"},
        new Foo {Id = "C", Bar = "C"}
    };

    public ElasticSearchStringTests(ElasticsearchResource resource) : base(resource)
    {
    }

    public class TestOperationType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        }
    }

    public class FooFilterType : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(x => x.Bar).Type<TestOperationType>();
            descriptor.Field(x => x.Id).Type<TestOperationType>();
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
            test(where: {bar: { eq: ""A"" }}) {
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

        public string Id { get; set; } = string.Empty;
    }
}
