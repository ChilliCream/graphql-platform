using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data;

public class IdFilterTypeInterceptorTests
{
    [Fact]
    public async Task Filtering_Should_UseIdType_When_Specified()
    {
        ISchema schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve("a"))
            .AddType(new FilterInputType<Foo>(x =>
                x.Field(y => y.Bar).Type<IdOperationFilterInputType>()))
            .AddFiltering()
            .BuildSchemaAsync();

        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Filtering_Should_InfereType_When_Annotated()
    {
        ISchema schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve("a"))
            .AddType(new FilterInputType<FooId>())
            .AddFiltering()
            .BuildSchemaAsync();

        schema.Print().MatchSnapshot();
    }

    public class Foo
    {
        public string? Bar { get; }
    }

    public class FooId
    {
        [ID]
        public string? Bar { get; }
    }
}
