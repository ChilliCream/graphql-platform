using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

// TODO: Do we need to verify the incoming Content-Type?
internal sealed class DynamicEndpointMiddleware(string schemaName, ExecutableOpenApiDocument document)
{
    private static readonly JsonWriterOptions s_jsonWriterOptions =
        new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly JsonSerializerOptions s_jsonSerializerOptions =
        new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // TODO: This needs to raise diagnostic events (httprequest, startsingle and httprequesterror
    public async Task InvokeAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;

        try
        {
            var proxy = context.RequestServices.GetRequiredKeyedService<HttpRequestExecutorProxy>(schemaName);
            var session = await proxy.GetOrCreateSessionAsync(context.RequestAborted);

            // TODO: Map to variables
            var routeData = context.GetRouteData();

            var requestBuilder = OperationRequestBuilder.New()
                .SetDocument(document.Document)
                .SetVariableValues(routeData.Values);

            await session.OnCreateAsync(context, requestBuilder, cancellationToken);

            var executionResult = await session.ExecuteAsync(
                requestBuilder.Build(),
                cancellationToken).ConfigureAwait(false);

            // If the request was cancelled, we do not attempt to write a response.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // If we do not have an operation result, something went wrong and we return HTTP 500.
            // TODO: We need to ensure these requests don't contain incremental directives like @stream and @defer
            if (executionResult is not IOperationResult operationResult)
            {
                await Results.InternalServerError().ExecuteAsync(context);
                return;
            }

            // If the request had validation errors or execution didn't start, we return HTTP 400.
            if (operationResult.ContextData?.ContainsKey(ExecutionContextData.ValidationErrors) == true
                || !operationResult.IsDataSet)
            {
                await Results.BadRequest().ExecuteAsync(context);
                return;
            }

            // If execution started and we produced GraphQL errors,
            // we return HTTP 500 or 401/403 for authorization errors.
            if (operationResult.Errors is not null)
            {
                var result = GetResultFromErrors(operationResult.Errors);

                await result.ExecuteAsync(context);
                return;
            }

            // If the root field is null and we don't have any errors,
            // we return HTTP 404 for queries and HTTP 500 otherwise.
            if (operationResult.Data?.TryGetValue(document.ResponseNameToExtract, out var responseData) != true
                || responseData is null)
            {
                var result = document.HttpMethod == HttpMethods.Get
                    ? Results.NotFound()
                    : Results.InternalServerError();

                await result.ExecuteAsync(context);
                return;
            }

            var bodyWriter = context.Response.BodyWriter;
            var jsonWriter = new Utf8JsonWriter(bodyWriter, s_jsonWriterOptions);

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";

            JsonValueFormatter.WriteValue(jsonWriter, responseData, s_jsonSerializerOptions,
                JsonNullIgnoreCondition.None);

            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await Results.InternalServerError().ExecuteAsync(context);
        }
    }

    private static IResult GetResultFromErrors(IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            if (error.Code == ErrorCodes.Authentication.NotAuthenticated)
            {
                return Results.Unauthorized();
            }

            if (error.Code == ErrorCodes.Authentication.NotAuthorized)
            {
                return Results.Forbid();
            }
        }

        return Results.InternalServerError();
    }
}
