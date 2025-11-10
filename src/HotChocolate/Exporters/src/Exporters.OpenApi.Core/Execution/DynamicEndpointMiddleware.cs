using HotChocolate.AspNetCore;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
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
            if (endpointDescriptor.VariableFilledThroughBody is not null)
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

            using var variableBuffer = new PooledArrayWriter();
            var variables = await BuildVariables(
                endpointDescriptor,
                context,
                variableBuffer,
                session.Schema,
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
            if (executionResult is not IOperationResult operationResult)
            {
                await Results.InternalServerError().ExecuteAsync(context);
                return;
            }

            // If the request had validation errors or execution didn't start, we return HTTP 400.
            if (operationResult.ContextData?.ContainsKey(ExecutionContextData.ValidationErrors) == true
                || operationResult is OperationResult { IsDataSet: false })
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
        catch
        {
            await Results.InternalServerError().ExecuteAsync(context);
        }
    }

    private static async Task<IReadOnlyDictionary<string, object?>?> BuildVariables(
        OpenApiEndpointDescriptor endpointDescriptor,
        HttpContext httpContext,
        PooledArrayWriter variableBuffer,
        ISchemaDefinition schema,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, object?>();

        if (endpointDescriptor.VariableFilledThroughBody is {} bodyVariable)
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

            var jsonValueParser = new JsonValueParser(buffer: variableBuffer);

            var bodyValue = jsonValueParser.Parse(writer.WrittenSpan);

            variables[bodyVariable] = bodyValue;
        }

        // TODO: Derive from parameter
        var stringType = schema.Types["String"];

        // TODO: Handle deeply nested objects
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

                variables[parameter.Variable] = ParseValueNode(value, stringType);
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

                variables[parameter.Variable] = ParseValueNode(values[0], stringType);
            }
        }

        return variables;
    }

    // TODO: This needs information about the underlying type of custom scalars like Long, etc.
    private static IValueNode? ParseValueNode(object? value, ITypeDefinition type)
    {
        if (value is null)
        {
            return null;
        }

        if (type is IEnumTypeDefinition enumType)
        {
            if (value is not string s)
            {
                throw new InvalidFormatException();
            }

            var matchingValue = enumType.Values.FirstOrDefault(v => v.Name == s);

            if (matchingValue is null)
            {
                throw new InvalidFormatException();
            }

            return new EnumValueNode(matchingValue.Name);
        }

        if (type.Name is "String" or "ID")
        {
            if (value is string s)
            {
                return new StringValueNode(s);
            }
        }
        else if (type.Name is "Boolean")
        {
            if (value is bool b)
            {
                return new BooleanValueNode(b);
            }

            if (value is string s && bool.TryParse(s, out var booleanValue))
            {
                return new BooleanValueNode(booleanValue);
            }
        }
        else if (type.Name == "Int")
        {
            if (value is int i)
            {
                return new IntValueNode(i);
            }

            if (value is string s && int.TryParse(s, out var intValue))
            {
                return new IntValueNode(intValue);
            }
        }
        else if (type.Name == "Float")
        {
            if (value is float f)
            {
                return new FloatValueNode(f);
            }

            if (value is double d)
            {
                return new FloatValueNode(d);
            }

            if (value is string s && double.TryParse(s, out var doubleValue))
            {
                return new FloatValueNode(doubleValue);
            }
        }

        if (value is string stringValue)
        {
            return new StringValueNode(stringValue);
        }

        throw new InvalidFormatException();
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
