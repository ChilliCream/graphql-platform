using System.Text.Json;
using System.Web;
using HotChocolate.OpenApi.Models;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Helpers;

internal static class OperationResolverHelper
{
    public static Func<IResolverContext, Task<JsonElement>> CreateResolverFunc(Operation operation)
    {
        return context => ResolveAsync(context, operation);
    }

    private static async Task<JsonElement> ResolveAsync(IResolverContext resolverContext, Operation operation)
    {
        var httpClient = resolverContext.Services
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("OpenApi");

        var request = CreateRequest(resolverContext, operation);
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Downstream request failed");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    private static HttpRequestMessage CreateRequest(IResolverContext resolverContext, Operation operation)
    {
        var path = operation.Path;
        var queryBuilder = HttpUtility.ParseQueryString(string.Empty);

        foreach (var operationArgument in operation.Arguments)
        {
            if (operationArgument.Parameter is { } parameter)
            {
                if (parameter.In == ParameterLocation.Path)
                {
                    path = path.Replace($"{{{parameter.Name}}}", resolverContext.ArgumentValue<string>(parameter.Name));
                }
            }
        }

        var request = new HttpRequestMessage(operation.Method, path);

        return request;
    }
}
