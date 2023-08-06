using System.Net.Http.Headers;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.OpenApi.Models;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
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
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    private static HttpRequestMessage CreateRequest(IResolverContext resolverContext, Operation operation)
    {
        var path = operation.Path;
        HttpContent? content = null;

        foreach (var operationArgument in operation.Arguments)
        {
            if (operationArgument.Parameter is { } parameter)
            {
                if (parameter.In == ParameterLocation.Path)
                {
                    path = path.Replace($"{{{parameter.Name}}}", resolverContext.ArgumentValue<string>(parameter.Name));
                }
            }

            if (operationArgument.RequestBody is not null)
            {
                var valueNode = resolverContext.ArgumentLiteral<IValueNode>("input");
                using var arrayWriter = new ArrayWriter();
                using var writer = new Utf8JsonWriter(arrayWriter);
                Utf8JsonWriterHelper.WriteValueNode(writer, valueNode);
                writer.Flush();

                content = new ByteArrayContent(arrayWriter.GetInternalBuffer(),0, arrayWriter.Length);
#if NET7_0_OR_GREATER
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json", "utf-8");
#else
                fileMap.Headers.ContentType = new MediaTypeHeaderValue(ContentType.Json) { CharSet = "utf-8" };
#endif
            }
        }

        var request = new HttpRequestMessage(operation.Method, path);

        if (content is not null)
        {
            request.Content = content;
        }

        return request;
    }
}
