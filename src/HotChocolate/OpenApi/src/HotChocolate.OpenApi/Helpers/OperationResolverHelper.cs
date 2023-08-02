using HotChocolate.OpenApi.Models;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.OpenApi.Helpers;

internal sealed class OperationResolverHelper
{
    public static Func<IResolverContext, Task<string>> CreateResolverFunc(Operation operation)
    {
        return context => ResolveAsync(context, operation);
    }

    private static async Task<string> ResolveAsync(IResolverContext resolverContext, Operation operation)
    {
        var httpClient = resolverContext.Services
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("OpenApi");

        var request = CreateRequest(resolverContext, operation);
        var response = await httpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    private static HttpRequestMessage CreateRequest(IResolverContext resolverContext, Operation operation)
    {
        var request = new HttpRequestMessage(operation.Method, operation.Path);

        return request;
    }
}
