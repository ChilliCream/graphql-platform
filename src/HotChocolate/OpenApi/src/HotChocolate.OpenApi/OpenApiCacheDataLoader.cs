using System.Text.Json;
using GreenDonut;
using HotChocolate.Resolvers;

namespace HotChocolate.OpenApi;

internal sealed class OpenApiCacheDataLoader(
    IResolverContext resolverContext,
    DataLoaderOptions options,
    string httpClientName,
    HttpRequestMessage request,
    OpenApiOperationWrapper operationWrapper)
    : CacheDataLoader<string, JsonElement>(options)
{
    protected override async Task<JsonElement> LoadSingleAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return await OpenApiResolverFactory.ExecuteRequest(
            resolverContext,
            httpClientName,
            request,
            operationWrapper,
            cancellationToken);
    }
}
