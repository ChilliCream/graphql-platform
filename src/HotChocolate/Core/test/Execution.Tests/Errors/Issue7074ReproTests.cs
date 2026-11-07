using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Errors;

public class Issue7074ReproTests
{
    [Fact]
    public async Task Aggregate_GraphQLException_Produces_Only_One_Field_Error()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync("{ test }");
        var json = result.ToJson();

        Assert.Contains("Test1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Test2", json, StringComparison.Ordinal);
    }

    public class Query
    {
        public string Test()
            => throw new AggregateException(
                new GraphQLException("Test1"),
                new GraphQLException("Test2"));
    }
}
