using HotChocolate.Execution.Caching;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class RequestExecutorResolverTests
{
    [Fact]
    public async Task Operation_Cache_Should_Be_Scoped_To_Executor()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IRequestExecutorResolver>();

        // act
        var firstExecutor = await resolver.GetRequestExecutorAsync();
        var firstOperationCache = firstExecutor.Services.GetCombinedServices()
            .GetRequiredService<IPreparedOperationCache>();

        resolver.EvictRequestExecutor();

        var secondExecutor = await resolver.GetRequestExecutorAsync();
        var secondOperationCache = secondExecutor.Services.GetCombinedServices()
            .GetRequiredService<IPreparedOperationCache>();

        // assert
        Assert.NotEqual(secondOperationCache, firstOperationCache);
    }
}
