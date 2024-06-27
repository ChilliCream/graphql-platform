using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using GreenDonut;
using HotChocolate.Language;
using HotChocolate.OpenApi.Helpers;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OperationType = Microsoft.OpenApi.Models.OperationType;

namespace HotChocolate.OpenApi;

internal static class OpenApiResolverFactory
{
    private static readonly JsonElement NullJsonElement = JsonDocument.Parse("null").RootElement;

    public static Func<IResolverContext, Task<JsonElement>> CreateResolver(
        string httpClientName,
        OpenApiOperationWrapper operationWrapper)
    {
        return async resolverContext =>
        {
            var request = CreateRequest(resolverContext, operationWrapper);

            // Cache GET requests.
            if (operationWrapper.Type is OperationType.Get)
            {
                return await new OpenApiCacheDataLoader(
                    resolverContext,
                    resolverContext.RequestServices.GetRequiredService<DataLoaderOptions>(),
                    httpClientName,
                    request,
                    operationWrapper).LoadAsync(request.RequestUri!.ToString());
            }

            return await ExecuteRequest(
                resolverContext,
                httpClientName,
                request,
                operationWrapper,
                resolverContext.RequestAborted);
        };
    }

    public static async Task<JsonElement> ExecuteRequest(
        IResolverContext resolverContext,
        string httpClientName,
        HttpRequestMessage request,
        OpenApiOperationWrapper operationWrapper,
        CancellationToken cancellationToken)
    {
        using var httpClient = resolverContext.Services
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(httpClientName);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        var statusCode = response.StatusCode.ToString("D");
        var openApiLinks = GetOpenApiLinks(operationWrapper.Operation.Responses, statusCode);

        resolverContext.ContextData[WellKnownContextData.OpenApiRequest] = request;
        resolverContext.ContextData[WellKnownContextData.OpenApiResponse] = response;
        resolverContext.ContextData[WellKnownContextData.OpenApiHttpStatusCode] = statusCode;
        resolverContext.ContextData[WellKnownContextData.OpenApiLinks] = openApiLinks;

        var responseBuffer = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        return responseBuffer.Length is 0
            ? NullJsonElement
            : GetJsonElement(responseBuffer);

        static JsonElement GetJsonElement(byte[] buffer)
        {
            var jsonReader = new Utf8JsonReader(buffer);

            return JsonElement.ParseValue(ref jsonReader);
        }
    }

    private static HttpRequestMessage CreateRequest(
        IResolverContext resolverContext,
        OpenApiOperationWrapper operationWrapper)
    {
        var operation = operationWrapper.Operation;
        var pathStringBuilder = new StringBuilder(operationWrapper.Path);
        var queryStringBuilder = new StringBuilder();

        var pathParameters = new Dictionary<string, object?>();
        var queryParameters = new Dictionary<string, object?>();

        foreach (var parameter in operation.Parameters)
        {
            var argumentValue = GetArgumentValue(parameter, resolverContext);

            switch (parameter.In)
            {
                case ParameterLocation.Path:
                    pathStringBuilder.Replace(
                        $"{{{parameter.Name}}}",
                        OpenApiParameterSerializer.SerializeParameter(
                            parameter,
                            argumentValue));

                    pathParameters.Add(parameter.Name, argumentValue);

                    break;

                case ParameterLocation.Query:
                    // Skip optional parameters.
                    if (argumentValue is null && !parameter.Required)
                    {
                        continue;
                    }

                    var serializedParameter =
                        OpenApiParameterSerializer.SerializeParameter(parameter, argumentValue);

                    queryStringBuilder.Append($"{serializedParameter}&");

                    queryParameters.Add(parameter.Name, argumentValue);

                    break;

                case ParameterLocation.Header:
                case ParameterLocation.Cookie:
                case null:
                    continue;

                default:
                    throw new InvalidOperationException();
            }
        }

        // Store parameters in resolver context data.
        resolverContext.ContextData[WellKnownContextData.OpenApiPathParameters] = pathParameters;
        resolverContext.ContextData[WellKnownContextData.OpenApiQueryParameters] = queryParameters;

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

        return new HttpRequestMessage(new HttpMethod(operationWrapper.Type.ToString()), requestUri)
        {
            Content = content,
        };
    }

    private static object? GetArgumentValue(
        OpenApiParameter parameter,
        IResolverContext resolverContext)
    {
        // Try to get the argument value from a link parameter.
        if (resolverContext.ContextData.TryGetValue(
                WellKnownContextData.OpenApiLinks,
                out var openApiLinks) &&
            openApiLinks is IDictionary<string, OpenApiLink> links &&
            links.TryGetValue(resolverContext.Selection.Field.Name, out var link) &&
            link.Parameters.TryGetValue(parameter.Name, out var parameterValue))
        {
            if (parameterValue.Any is { } any)
            {
                return GetOpenApiAnyValue(any);
            }

            var request = GetContextData<HttpRequestMessage>(
                resolverContext,
                WellKnownContextData.OpenApiRequest)!;

            var pathParameters = GetContextData<IReadOnlyDictionary<string, object?>>(
                resolverContext,
                WellKnownContextData.OpenApiPathParameters)!;

            var queryParameters = GetContextData<IReadOnlyDictionary<string, object?>>(
                resolverContext,
                WellKnownContextData.OpenApiPathParameters)!;

            var response = GetContextData<HttpResponseMessage>(
                resolverContext,
                WellKnownContextData.OpenApiResponse)!;

            return RuntimeExpressionEvaluator.EvaluateExpression(
                parameterValue.Expression,
                parameter,
                request,
                pathParameters,
                queryParameters,
                response,
                responseBody: resolverContext.Parent<JsonElement>());
        }

        return resolverContext.ArgumentValue<object?>(parameter.Name);
    }

    private static object? GetOpenApiAnyValue(IOpenApiAny any)
    {
        return any switch
        {
            OpenApiArray a => GetOpenApiAnyArrayValue(a),
            OpenApiBinary b => b.Value,
            OpenApiBoolean b => b.Value,
            OpenApiByte b => b.Value,
            OpenApiDate d => d.Value,
            OpenApiDateTime d => d.Value,
            OpenApiDouble d => d.Value,
            OpenApiFloat f => f.Value,
            OpenApiInteger i => i.Value,
            OpenApiLong l => l.Value,
            OpenApiNull => null,
            OpenApiObject o => GetOpenApiAnyObjectValue(o),
            OpenApiPassword p => p.Value,
            OpenApiString s => s.Value,
            _ => throw new InvalidOperationException(),
        };
    }

    private static List<object?> GetOpenApiAnyArrayValue(OpenApiArray openApiArray)
    {
        var result = new List<object?>();

        foreach (var item in openApiArray)
        {
            result.Add(GetOpenApiAnyValue(item));
        }

        return result;
    }

    private static Dictionary<string, object?> GetOpenApiAnyObjectValue(OpenApiObject openApiObject)
    {
        var result = new Dictionary<string, object?>();

        foreach (var (key, value) in openApiObject)
        {
            result.Add(key, GetOpenApiAnyValue(value));
        }

        return result;
    }

    private static T? GetContextData<T>(IHasContextData resolverContext, string name)
    {
        return (T?)resolverContext.ContextData[name];
    }

    private static IDictionary<string, OpenApiLink> GetOpenApiLinks(
        OpenApiResponses openApiResponses,
        string httpStatusCode)
    {
        // Direct match (200 = 200).
        if (openApiResponses.TryGetValue(httpStatusCode, out var openApiResponse1))
        {
            return openApiResponse1.Links;
        }

        // Wildcard match (200 = 2XX).
        if (openApiResponses.TryGetValue(httpStatusCode[0] + "XX", out var openApiResponse2))
        {
            return openApiResponse2.Links;
        }

        // Default match (200 = default).
        return openApiResponses.TryGetValue("default", out var openApiResponse3)
            ? openApiResponse3.Links
            : new Dictionary<string, OpenApiLink>();
    }
}
