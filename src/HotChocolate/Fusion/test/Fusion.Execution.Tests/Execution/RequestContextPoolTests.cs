using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public sealed class RequestContextPoolTests : FusionTestBase
{
    [Fact]
    public async Task Get_Should_ReuseReturnedContexts_When_64ContextsWereReturned()
    {
        // arrange
        const int inFlight = 64;
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String!
                    }
                    """));
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var pool = executor.Schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
        var returned = new HashSet<PooledRequestContext>();

        for (var i = 0; i < inFlight; i++)
        {
            returned.Add(pool.Get());
        }

        foreach (var context in returned)
        {
            pool.Return(context);
        }

        // act
        var reused = 0;

        for (var i = 0; i < inFlight; i++)
        {
            if (returned.Contains(pool.Get()))
            {
                reused++;
            }
        }

        // assert
        Assert.Equal(inFlight, reused);
    }
}
