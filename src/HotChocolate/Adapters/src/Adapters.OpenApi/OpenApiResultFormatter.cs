using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class OpenApiResultFormatter : IOpenApiResultFormatter
{
    private static readonly JsonWriterOptions s_jsonWriterOptions =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly JsonSerializerOptions s_jsonSerializerOptions =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public async Task FormatResultAsync(
        OperationResult operationResult,
        HttpContext httpContext,
        OpenApiEndpointDescriptor endpoint,
        CancellationToken cancellationToken)
    {
        if (operationResult.Data?.Value is not ResultDocument resultDocument)
        {
            await Results.InternalServerError().ExecuteAsync(httpContext);
            return;
        }

        if (!resultDocument.Data.TryGetProperty(endpoint.ResponseNameToExtract, out var rootProperty))
        {
            await Results.InternalServerError().ExecuteAsync(httpContext);
            return;
        }

        // If the root field is null, and we don't have any errors,
        // we return HTTP 404 for queries and HTTP 500 otherwise.
        if (rootProperty.IsNullOrInvalidated)
        {
            var result = endpoint.HttpMethod == HttpMethods.Get
                ? Results.NotFound()
                : Results.InternalServerError();

            await result.ExecuteAsync(httpContext);
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = "application/json";

        var bodyWriter = httpContext.Response.BodyWriter;

        rootProperty.WriteTo(bodyWriter);

        await bodyWriter.FlushAsync(cancellationToken);
    }
}
