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
        var resolver = provider.GetRequiredService<IRequestExecutorProvider>();

        // act
        var executor = await resolver.GetExecutorAsync();
        var operationCache = executor.Schema.Services.GetCombinedServices()
            .GetRequiredService<IPreparedOperationCache>();

        // assert
        Assert.Equal(operationCache.Capacity, operationCacheCapacity);
    }
}
