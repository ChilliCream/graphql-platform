using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class ShareableTests
{
    [Fact]
    public static async Task PageInfo_Is_Shareable()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddTypeExtension(typeof(PageInfoExtensions))
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task PageInfo_Is_Shareable_Fluent()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddTypeExtension(
                    new ObjectTypeExtension(d =>
                    {
                        d.Name("PageInfo");
                        d.Shareable();
                    }))
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task New_On_PageInfo_Is_Shareable()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddTypeExtension(typeof(PageInfoScopedExtensions))
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task New_On_PageInfo_Is_Shareable_Fluent()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddTypeExtension(
                    new ObjectTypeExtension(d =>
                    {
                        d.Name("PageInfo");
                        d.Shareable(scoped: true);
                        d.Field("new").Resolve("bar");
                    }))
                .BuildSchemaAsync();

        schema.MatchSnapshot();
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

    [Shareable(scoped: true)]
    [ExtendObjectType("PageInfo")]
    public static class PageInfoScopedExtensions
    {
        public static int New() => throw new NotImplementedException();
    }
}
