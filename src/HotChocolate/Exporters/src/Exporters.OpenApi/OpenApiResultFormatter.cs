using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class OpenApiResultFormatter : IOpenApiResultFormatter
{
    private static readonly JsonWriterOptions s_jsonWriterOptions =
        new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly JsonSerializerOptions s_jsonSerializerOptions =
        new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public async Task FormatResultAsync(
        IOperationResult operationResult,
        HttpContext httpContext,
        OpenApiEndpointDescriptor endpoint,
        CancellationToken cancellationToken)
    {
        // If the root field is null and we don't have any errors,
        // we return HTTP 404 for queries and HTTP 500 otherwise.
        if (operationResult.Data?.TryGetValue(endpoint.ResponseNameToExtract, out var responseData) != true
            || responseData is null)
        {
            var result = endpoint.HttpMethod == HttpMethods.Get
                ? Results.NotFound()
                : Results.InternalServerError();

            await result.ExecuteAsync(httpContext);
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = "application/json";

        var jsonWriter = new Utf8JsonWriter(httpContext.Response.BodyWriter, s_jsonWriterOptions);

        JsonValueFormatter.WriteValue(jsonWriter, responseData, s_jsonSerializerOptions,
            JsonNullIgnoreCondition.None);

        await jsonWriter.FlushAsync(cancellationToken);
    }
}
