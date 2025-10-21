using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class DynamicEndpointMiddleware(string schemaName, ExecutableOpenApiDocument document)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;

        // TODO: Use proxy here
        var provider = context.RequestServices.GetRequiredService<IRequestExecutorProvider>();
        var executor = await provider.GetExecutorAsync(schemaName, cancellationToken).ConfigureAwait(false);

        // TODO: Map to variables
        var routeData = context.GetRouteData();

        var request = OperationRequestBuilder.New()
            .SetDocument(document.Document)
            .SetVariableValues(routeData.Values)
            .Build();

        try
        {
            var result = await executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                // TODO: Handle properly
                return;
            }

            if (result is not IOperationResult operationResult)
            {
                await Results.StatusCode(500).ExecuteAsync(context);
                return;
            }

            // TODO: Handle errors

            var jsonWriterOptions = new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            if (!operationResult.IsDataSet)
            {
                await Results.StatusCode(500).ExecuteAsync(context);
                return;
            }

            if (operationResult.Data?.TryGetValue(document.ResponseNameToExtract, out var responseData) != true
                || responseData is null)
            {
                var statusCode = document.HttpMethod == HttpMethods.Get ? 404 : 500;

                await Results.StatusCode(statusCode).ExecuteAsync(context);
                return;
            }

            var bodyWriter = context.Response.BodyWriter;
            // TODO: Cache the writer
            var jsonWriter = new Utf8JsonWriter(bodyWriter, jsonWriterOptions);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";

            JsonValueFormatter.WriteValue(jsonWriter, responseData, jsonSerializerOptions, JsonNullIgnoreCondition.None);

            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await Results.StatusCode(500).ExecuteAsync(context);
        }
    }
}
