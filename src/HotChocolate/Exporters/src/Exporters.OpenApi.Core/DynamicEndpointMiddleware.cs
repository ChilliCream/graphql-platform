using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class DynamicEndpointMiddleware(
    string schemaName,
    OpenApiEndpointDescriptor endpointDescriptor)
{
    // TODO: This needs to raise diagnostic events (httprequest, startsingle and httprequesterror
    public async Task InvokeAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;

        try
        {
            if (endpointDescriptor.BodyParameter is not null)
            {
                if (context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) != true)
                {
                    await Results.Problem(
                        detail: "Content-Type must be application/json",
                        statusCode: StatusCodes.Status415UnsupportedMediaType).ExecuteAsync(context);

                    return;
                }

                if (context.Request.ContentLength < 1)
                {
                    await Results.Problem(
                        detail: "Request body is required",
                        statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(context);
                    return;
                }
            }

            var proxy = context.RequestServices.GetRequiredKeyedService<HttpRequestExecutorProxy>(schemaName);
            var formatter = context.RequestServices.GetRequiredService<IOpenApiResultFormatter>();
            var session = await proxy.GetOrCreateSessionAsync(context.RequestAborted);

            var variables = await BuildVariables(
                endpointDescriptor,
                context,
                cancellationToken);

            var requestBuilder = OperationRequestBuilder.New()
                .SetDocument(endpointDescriptor.Document)
                .SetErrorHandlingMode(ErrorHandlingMode.Halt)
                .SetVariableValues(variables);

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
                // TODO: This is wrong for Fusion
                // || !operationResult.IsDataSet
                )
            {
                await Results.BadRequest().ExecuteAsync(context);
                return;
            }

            // If execution started, and we produced GraphQL errors,
            // we return HTTP 500 or 401/403 for authorization errors.
            if (operationResult.Errors is not null)
            {
                var result = GetResultFromErrors(operationResult.Errors);

                await result.ExecuteAsync(context);
                return;
            }

            await formatter.FormatResultAsync(operationResult, context, endpointDescriptor, cancellationToken);
        }
        catch (Exception e)
        {
            await Results.InternalServerError(e).ExecuteAsync(context);
        }
    }

    private static async Task<IReadOnlyDictionary<string, object?>?> BuildVariables(
        OpenApiEndpointDescriptor endpointDescriptor,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, object?>();

        if (endpointDescriptor.BodyParameter is not null)
        {
            const int chunkSize = 256;
            using var writer = new PooledArrayWriter();
            var body = httpContext.Request.Body;
            int read;

            do
            {
                var memory = writer.GetMemory(chunkSize);
                read = await body.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                writer.Advance(read);

                // if (_maxRequestSize < writer.Length)
                // {
                //     throw DefaultHttpRequestParser_MaxRequestSizeExceeded();
                // }
            } while (read == chunkSize);

            if (read == 0)
            {
                throw new InvalidOperationException("Expected to have a body");
            }

            var variablesProperty = GetObjectProperty(
                variables,
                endpointDescriptor.BodyParameter.Variable);

            ParseJsonIntoDictionary(variablesProperty, writer.WrittenSpan);
        }

        if (endpointDescriptor.RouteParameters.Count > 0)
        {
            var routeData = httpContext.GetRouteData();

            foreach (var parameter in endpointDescriptor.RouteParameters)
            {
                if (!routeData.Values.TryGetValue(parameter.Key, out var value))
                {
                    // We just skip here and let the GraphQL execution take care of the validation
                    continue;
                }

                MapIntoVariables(variables, parameter, value);
            }
        }

        if (endpointDescriptor.QueryParameters.Count > 0)
        {
            foreach (var parameter in endpointDescriptor.QueryParameters)
            {
                if (!httpContext.Request.Query.TryGetValue(parameter.Key, out var values))
                {
                    // We just skip here and let the GraphQL execution take care of the validation
                    continue;
                }

                var value = ParseQueryStringValue(values[0]);

                MapIntoVariables(variables, parameter, value);
            }
        }

        return variables;
    }

    private static void MapIntoVariables(
        Dictionary<string, object?> variables,
        OpenApiRouteSegmentParameter parameter,
        object? value)
    {
        if (parameter.InputObjectPath.HasValue)
        {
            var objectProperty = GetObjectProperty(variables, parameter.Variable);
            var path = parameter.InputObjectPath.Value;

            for (var i = 0; i < path.Length - 1; i++)
            {
                objectProperty = GetObjectProperty(objectProperty, path[i]);
            }

            objectProperty[path[^1]] = value;
        }
        else
        {
            variables[parameter.Variable] = value;
        }
    }

    private static Dictionary<string, object?> GetObjectProperty(
        Dictionary<string, object?> @object,
        string key)
    {
        if (!@object.TryGetValue(key, out var existing))
        {
            var newObject = new Dictionary<string, object?>();
            @object[key] = newObject;
            return newObject;
        }

        if (existing is Dictionary<string, object?> existingDict)
        {
            return existingDict;
        }

        throw new InvalidOperationException($"Path segment '{key}' is not an object");
    }

    // TODO: This is incorrect. The parsing should be done based on the type it's mapping to. Could be they want the true literal string...
    private static object? ParseQueryStringValue(string? value)
    {
        if (value is "true")
        {
            return true;
        }

        if (value is "false")
        {
            return false;
        }

        if (int.TryParse(value, out var intValue))
        {
            return intValue;
        }

        if (double.TryParse(value, out var doubleValue))
        {
            return doubleValue;
        }

        return value;
    }

    // TODO: This json stuff should live elsewhere
    // TODO: We should only parse the properties that are actually required, otherwise this could be used to try to sneak in stuff
    private static void ParseJsonIntoDictionary(
        Dictionary<string, object?> dictionary,
        ReadOnlySpan<byte> utf8Json)
    {
        var reader = new Utf8JsonReader(utf8Json);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected JSON object");
        }

        ParseObject(ref reader, dictionary);
    }

    private static void ParseObject(ref Utf8JsonReader reader, Dictionary<string, object?> dictionary)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name");
            }

            var propertyName = reader.GetString()!;
            reader.Read();

            dictionary[propertyName] = ParseValue(ref reader);
        }
    }

    private static object? ParseValue(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var l) ? l : reader.GetDouble(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.StartObject => ParseObjectValue(ref reader),
            JsonTokenType.StartArray => ParseArrayValue(ref reader),
            _ => throw new JsonException($"Unexpected token: {reader.TokenType}")
        };
    }

    private static Dictionary<string, object?> ParseObjectValue(ref Utf8JsonReader reader)
    {
        var obj = new Dictionary<string, object?>();
        ParseObject(ref reader, obj);
        return obj;
    }

    private static List<object?> ParseArrayValue(ref Utf8JsonReader reader)
    {
        var array = new List<object?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return array;
            }

            array.Add(ParseValue(ref reader));
        }

        throw new JsonException("Unexpected end of array");
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
