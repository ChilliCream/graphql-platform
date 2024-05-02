using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.OpenApi.Helpers;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using OperationType = Microsoft.OpenApi.Models.OperationType;

namespace HotChocolate.OpenApi;

internal static class OpenApiResolverFactory
{
    private static readonly JsonElement NullJsonElement = JsonDocument.Parse("null").RootElement;

    public static Func<IResolverContext, Task<JsonElement>> CreateResolver(
        string httpClientName,
        OperationType operationType,
        string path,
        OpenApiOperation operation)
    {
        return async resolverContext =>
        {
            var httpClient = resolverContext.Services
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient(httpClientName);

            var request = CreateRequest(resolverContext, path, operationType, operation);

            var response = await httpClient.SendAsync(request, resolverContext.RequestAborted);

            // Store the HTTP status code in resolver context data.
            resolverContext.ContextData.Add(
                WellKnownContextData.OpenApiHttpStatusCode,
                response.StatusCode.ToString("D"));

            var responseBuffer = await response.Content.ReadAsByteArrayAsync(
                resolverContext.RequestAborted);

            return responseBuffer.Length is 0
                ? NullJsonElement
                : GetJsonElement(responseBuffer);

            JsonElement GetJsonElement(byte[] buffer)
            {
                var jsonReader = new Utf8JsonReader(buffer);

                return JsonElement.ParseValue(ref jsonReader);
            }
        };
    }

    private static HttpRequestMessage CreateRequest(
        IPureResolverContext resolverContext,
        string path,
        OperationType operationType,
        OpenApiOperation operation)
    {
        var pathStringBuilder = new StringBuilder(path);
        var queryStringBuilder = new StringBuilder();

        foreach (var parameter in operation.Parameters)
        {
            switch (parameter.In)
            {
                case ParameterLocation.Path:
                    pathStringBuilder.Replace(
                        $"{{{parameter.Name}}}",
                        OpenApiParameterSerializer.SerializeParameter(
                            parameter,
                            resolverContext.ArgumentValue<object?>(parameter.Name)));

                    break;

                case ParameterLocation.Query:
                    var value = resolverContext.ArgumentValue<object?>(parameter.Name);

                    // Skip optional parameters.
                    if (value is null && !parameter.Required)
                    {
                        continue;
                    }

                    var serializedParameter =
                        OpenApiParameterSerializer.SerializeParameter(parameter, value);

                    queryStringBuilder.Append($"{serializedParameter}&");

                    break;

                case ParameterLocation.Header:
                case ParameterLocation.Cookie:
                case null:
                    continue;

                default:
                    throw new InvalidOperationException();
            }
        }

        // Content
        HttpContent? content = null;

        if (operation.RequestBody is not null &&
            resolverContext.Selection.Field.ContextData.TryGetValue(
                WellKnownContextData.OpenApiInputFieldName, out var inputFieldName))
        {
            var input = resolverContext.ArgumentLiteral<IValueNode>((string)inputFieldName!);

            using var arrayWriter = new ArrayWriter();
            using var writer = new Utf8JsonWriter(arrayWriter);

            Utf8JsonWriterHelper.WriteValueNode(writer, input);
            writer.Flush();

            content = new ReadOnlyMemoryContent(arrayWriter.GetWrittenMemory());
            content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
        }

        var requestUri = string.Join(
            '?',
            pathStringBuilder,
            queryStringBuilder.ToString().TrimEnd('&')).TrimEnd('?');

        return new HttpRequestMessage(new HttpMethod(operationType.ToString()), requestUri)
        {
            Content = content,
        };
    }
}
