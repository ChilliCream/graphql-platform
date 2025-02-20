using HotChocolate.Execution.Caching;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class PreparedOperationCacheTests
{
    [Fact]
    public async Task Operation_Cache_Should_Have_Configured_Capacity()
    {
        // arrange
        var operationCacheCapacity = 517;
        var services = new ServiceCollection();
        services.AddOperationCache(operationCacheCapacity);
        services
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IRequestExecutorResolver>();

        // act
        var executor = await resolver.GetRequestExecutorAsync();
        var operationCache = executor.Services.GetCombinedServices()
            .GetRequiredService<IPreparedOperationCache>();

        // assert
        Assert.Equal(operationCache.Capacity, operationCacheCapacity);
    }
}
