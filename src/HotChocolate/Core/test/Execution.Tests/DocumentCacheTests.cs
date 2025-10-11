using HotChocolate.Execution.Caching;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DocumentCacheTests
{
    [Fact]
    public async Task Document_Cache_Should_Have_Configured_Capacity()
    {
        // arrange
        const int cacheCapacity = 517;
        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .ModifyOptions(o => o.OperationDocumentCacheSize = cacheCapacity)
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync();

        // act
        var documentCache = executor.Schema.Services.GetRequiredService<IDocumentCache>();

        // assert
        Assert.Equal(cacheCapacity, documentCache.Capacity);
    }
}
