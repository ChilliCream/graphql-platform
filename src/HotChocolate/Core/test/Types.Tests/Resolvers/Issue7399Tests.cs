using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Resolvers;

public class Issue7399Tests
{
    [Fact]
    public async Task AddParameterExpressionBuilder_For_GlobalState_Does_Not_AutoInject_Cost()
    {
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddParameterExpressionBuilder(
                ctx => ctx.GetGlobalState<MyGlobalState>("MyGlobalState"))
            .BuildRequestExecutorAsync();

        Assert.DoesNotContain("@cost(", executor.Schema.ToString(), StringComparison.Ordinal);
    }

    public sealed record MyGlobalState;

    public sealed record MyThing(string Id);

    public sealed class Query
    {
        public MyThing Test(MyGlobalState state) => new("Foo");
    }
}
