using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class Issue6871Tests
{
    [Fact]
    public async Task Mismatching_Implicit_Connection_Name_Should_Fail_Schema_Build()
    {
        var exception = await Record.ExceptionAsync(
            async () =>
            {
                _ = await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider()
                    .GetSchemaAsync();
            });

        Assert.IsType<SchemaException>(exception);
    }

    public sealed class Hero
    {
        [UsePaging(IncludeTotalCount = true)]
        public IEnumerable<Hero> GetFriends() => [];
    }

    public sealed class Villain
    {
        [UsePaging(RequirePagingBoundaries = true)]
        public IEnumerable<Villain> GetFriends() => [];
    }

    public sealed class Query
    {
        public IEnumerable<Hero> GetHeroes() => [];

        public IEnumerable<Villain> GetVillains() => [];
    }
}
