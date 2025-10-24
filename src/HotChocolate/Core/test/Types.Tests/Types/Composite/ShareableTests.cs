using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class ShareableTests
{
    [Fact]
    public static async Task Lookup()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddTypeExtension(typeof(PageInfoExtensions))
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """

            """);
    }

    public class Query1
    {
        [UsePaging]
        public IQueryable<string> GetNames()
            => throw new NotImplementedException();
    }

    [Shareable]
    [ExtendObjectType("PageInfo")]
    public static class PageInfoExtensions;
}
