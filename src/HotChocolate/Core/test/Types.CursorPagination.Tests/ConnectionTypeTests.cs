using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class ConnectionTypeTests
{
    [Fact]
    public async Task Fields_Should_BeParallelExecutable_When_GlobalFieldMiddlewareIsRegistered()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .UseField(next => next)
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var fields = schema.Types.GetType<ObjectType>("NamesConnection").Fields
            .Where(field => !field.IsIntrospectionField)
            .Select(field => $"{field.Name}: {field.IsParallelExecutable}");

        // assert
        string.Join("\n", fields).MatchInlineSnapshot(
            """
            pageInfo: True
            edges: True
            nodes: True
            totalCount: True
            """);
    }

    public class Query
    {
        [UsePaging(IncludeTotalCount = true)]
        public IEnumerable<string> GetNames() => ["a", "b"];
    }
}
