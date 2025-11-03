using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Results;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class FusionOpenApiResultFormatter : IOpenApiResultFormatter
{
    public async Task FormatResultAsync(
        IOperationResult operationResult,
        HttpContext httpContext,
        OpenApiEndpointDescriptor endpoint,
        CancellationToken cancellationToken)
    {
        if (operationResult is not RawOperationResult rawOperationResult)
        {
            await Results.InternalServerError().ExecuteAsync(httpContext);
            return;
        }

        if (!rawOperationResult.Result.Data.TryGetProperty(endpoint.ResponseNameToExtract, out var rootProperty))
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
