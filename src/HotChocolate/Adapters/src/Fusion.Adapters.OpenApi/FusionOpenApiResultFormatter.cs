using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class FusionOpenApiResultFormatter : IOpenApiResultFormatter
{
    public async Task FormatResultAsync(
        OperationResult operationResult,
        HttpContext httpContext,
        OpenApiEndpointDescriptor endpoint,
        CancellationToken cancellationToken)
    {
        if (operationResult.Data?.Value is not CompositeResultDocument resultDocument)
        {
            await Results.InternalServerError().ExecuteAsync(httpContext);
            return;
        }

        var responseValue = resultDocument.Data;

        foreach (var segment in endpoint.ResponseBodySelection.ResponseNamePath)
        {
            if (responseValue.ValueKind is not JsonValueKind.Object
                || !responseValue.TryGetProperty(segment, out responseValue))
            {
                await Results.InternalServerError().ExecuteAsync(httpContext);
                return;
            }

            // If any field on the response name path is null, and we don't have any errors,
            // we return HTTP 404 for queries and HTTP 500 otherwise.
            if (responseValue.IsNullOrInvalidated)
            {
                var result = endpoint.HttpMethod == HttpMethods.Get
                    ? Results.NotFound()
                    : Results.InternalServerError();

                await result.ExecuteAsync(httpContext);
                return;
            }
        }

        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = "application/json";

        var bodyWriter = httpContext.Response.BodyWriter;

        responseValue.WriteTo(bodyWriter);

        await bodyWriter.FlushAsync(cancellationToken);
    }
}
