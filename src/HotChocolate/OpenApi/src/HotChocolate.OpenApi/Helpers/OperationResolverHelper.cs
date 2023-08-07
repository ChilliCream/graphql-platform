using System.Net.Http.Headers;
using System.Text;
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
        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        var isValidNullResult = contentBytes.Length == 0 &&
                                operation.Response?.Reference is null;
        return isValidNullResult
            ? JsonDocument.Parse("""{"success": true}""").RootElement
            : JsonDocument.Parse(contentBytes).RootElement;
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
                    var pathValue = operation.Method == HttpMethod.Get
                        ? resolverContext.ArgumentValue<string>(parameter.Name)
                        : GetValueOfValueNode(resolverContext.ArgumentLiteral<IValueNode>("input"), parameter.Name);
                    path = path.Replace($"{{{parameter.Name}}}", pathValue );
                }
            }

            if (operationArgument.RequestBody is not null)
            {
                var valueNode = resolverContext.ArgumentLiteral<IValueNode>("input");
                var json = GetJsonValueOfInputNode(valueNode);
                content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
            }
        }

        var request = new HttpRequestMessage(operation.Method, path);

        if (content is not null)
        {
            request.Content = content;
        }

        return request;
    }

    private static string GetJsonValueOfInputNode(IValueNode valueNode)
    {
        using var arrayWriter = new ArrayWriter();
        using var writer = new Utf8JsonWriter(arrayWriter);
        Utf8JsonWriterHelper.WriteValueNode(writer, valueNode);
        writer.Flush();
        return Encoding.UTF8.GetString(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
    }

    private static string GetValueOfValueNode(IValueNode input, string fieldName)
    {
        var json = GetJsonValueOfInputNode(input);
        return JsonDocument.Parse(json).RootElement.GetProperty(fieldName).GetRawText();
    }
}
