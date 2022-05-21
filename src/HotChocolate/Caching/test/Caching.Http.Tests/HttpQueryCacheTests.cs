using System.Threading.Tasks;
using HotChocolate.Caching.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Http.Tests;

public class HttpQueryCacheTests : CacheControlTestBase
{
    [Fact(Skip = "Until reading is enabled")]
    public async Task ShouldReadResultFromCache_ReturnsFalse()
    {
        var (builder, cache) = GetExecutorBuilderAndCache<HttpQueryCache>();

        builder.AddQueryType(d => d.Name("Query")
            .Field("field").Resolve(""));

        await ExecuteRequestAsync(builder, "{ field }");

        AssertNoReadsFromCache(cache);
    }
}
