using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Adapters.OpenApi;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class DynamicEndpointMiddleware(
    string schemaName,
    OpenApiEndpointDescriptor endpointDescriptor)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;

        try
        {
            // If the document is invalid, we always return a HTTP 500, since a HTTP 400 would be confusing.
            if (!endpointDescriptor.HasValidDocument)
            {
#if NET9_0_OR_GREATER
                await Results.InternalServerError().ExecuteAsync(context);
#else
                await Results.StatusCode(500).ExecuteAsync(context);
#endif
                return;
            }

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
            var session = await proxy.GetOrCreateSessionAsync(context.RequestAborted);

            using var variableBuffer = new PooledArrayWriter();
            using var variables = await BuildVariablesAsync(
                endpointDescriptor,
                context,
                variableBuffer,
                cancellationToken);

            var requestBuilder = OperationRequestBuilder.New()
                .SetDocument(endpointDescriptor.Document)
                .SetErrorHandlingMode(ErrorHandlingMode.Halt)
                .SetVariableValues(variables);

            await session.OnCreateAsync(context, requestBuilder, cancellationToken);

            var executionResult = await session.ExecuteAsync(
                requestBuilder.Build(),
                cancellationToken).ConfigureAwait(false);

            // If the request was canceled, we do not attempt to write a response.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // If we do not have an operation result, something went wrong, and we return HTTP 500.
            if (executionResult is not OperationResult operationResult)
            {
#if NET9_0_OR_GREATER
                await Results.InternalServerError().ExecuteAsync(context);
#else
                await Results.StatusCode(500).ExecuteAsync(context);
#endif
                return;
            }

            // If the request had validation errors or execution didn't start, we return HTTP 400.
            if (operationResult.ContextData.ContainsKey(ExecutionContextData.ValidationErrors)
                || !operationResult.Data.HasValue)
            {
                var firstErrorMessage = operationResult.Errors.FirstOrDefault()?.Message;

                if (!string.IsNullOrEmpty(firstErrorMessage))
                {
                    await Results.Problem(
                        detail: firstErrorMessage,
                        statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(context);
                }
                else
                {
                    await Results.BadRequest().ExecuteAsync(context);
                }

                return;
            }

            // If execution started, and we produced GraphQL errors,
            // we return HTTP 500 or 401/403 for authorization errors.
            if (!operationResult.Errors.IsEmpty)
            {
                var result = GetResultFromErrors(operationResult.Errors);

                await result.ExecuteAsync(context);
                return;
            }

            var formatter = session.Schema.Services.GetRequiredService<IOpenApiResultFormatter>();

            await formatter.FormatResultAsync(operationResult, context, endpointDescriptor, cancellationToken);
        }
        catch (BadRequestException badRequestException)
        {
            await Results.Problem(
                detail: badRequestException.Message,
                statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(context);
        }
        catch
        {
#if NET9_0_OR_GREATER
            await Results.InternalServerError().ExecuteAsync(context);
#else
            await Results.StatusCode(500).ExecuteAsync(context);
#endif
        }
    }

    private static async Task<JsonDocument> BuildVariablesAsync(
        OpenApiEndpointDescriptor endpointDescriptor,
        HttpContext httpContext,
        PooledArrayWriter variableBuffer,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, IValueNode?>();

        if (endpointDescriptor.VariableFilledThroughBody is { } bodyVariable)
        {
            var body = httpContext.Request.BodyReader;
            ReadResult result;

            do
            {
                result = await body.ReadAsync(cancellationToken);
                body.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            } while (result is { IsCompleted: false, IsCanceled: false });

            if (result.IsCanceled)
            {
                throw new OperationCanceledException();
            }

            if (result.Buffer.Length == 0)
            {
                throw new BadRequestException("Expected to have a body");
            }

            var jsonValueParser = new JsonValueParser(buffer: variableBuffer);
            var bodyValue = jsonValueParser.Parse(result.Buffer);
            variables[bodyVariable] = bodyValue;
            body.AdvanceTo(result.Buffer.End);
        }

        InsertParametersIntoVariables(variables, endpointDescriptor, httpContext);

        var start = variableBuffer.Length;
        await using var writer = new Utf8JsonWriter(variableBuffer, new JsonWriterOptions { Indented = false });

        writer.WriteStartObject();

        foreach (var (key, value) in variables)
        {
            writer.WritePropertyName(key);

            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                ValueJsonFormatter.Format(writer, value);
            }
        }

        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken);

        return JsonDocument.Parse(variableBuffer.WrittenMemory[start..]);
    }

    private static void InsertParametersIntoVariables(
        Dictionary<string, IValueNode?> variables,
        OpenApiEndpointDescriptor endpointDescriptor,
        HttpContext httpContext)
    {
        var routeData = httpContext.GetRouteData();
        var query = httpContext.Request.Query;

        foreach (var (variableName, segment) in endpointDescriptor.ParameterTrie)
        {
            if (segment is VariableValueInsertionTrieLeaf leaf)
            {
                if (TryGetValueForParameter(leaf, routeData, query, out var value))
                {
                    variables[variableName] = value;
                }
            }
            else if (segment is VariableValueInsertionTrie trie
                && variables[variableName] is ObjectValueNode objectValue)
            {
                variables[variableName] = RewriteObjectValueNode(
                    objectValue,
                    trie,
                    routeData,
                    query);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    private static IValueNode RewriteObjectValueNode(
        ObjectValueNode objectValueNode,
        VariableValueInsertionTrie trie,
        RouteData routeData,
        IQueryCollection query)
    {
        var newFields = new List<ObjectFieldNode>();
        var processedFields = new HashSet<string>(objectValueNode.Fields.Count);

        foreach (var field in objectValueNode.Fields)
        {
            var fieldName = field.Name.Value;

            if (trie.TryGetValue(fieldName, out var segment))
            {
                if (segment is not VariableValueInsertionTrie trieSegment)
                {
                    throw new BadRequestException($"Unknown field '{fieldName}'");
                }

                if (field.Value is not ObjectValueNode fieldObject)
                {
                    throw new BadRequestException($"Expected field '{fieldName}' to be an object");
                }

                var newFieldValue = RewriteObjectValueNode(fieldObject, trieSegment, routeData, query);

                newFields.Add(field.WithValue(newFieldValue));
            }
            else
            {
                newFields.Add(field);
            }

            processedFields.Add(fieldName);
        }

        foreach (var (fieldName, segment) in trie)
        {
            if (processedFields.Contains(fieldName))
            {
                continue;
            }

            IValueNode newValue;
            if (segment is VariableValueInsertionTrieLeaf leaf)
            {
                if (!TryGetValueForParameter(leaf, routeData, query, out var value))
                {
                    continue;
                }

                newValue = value;
            }
            else if (segment is VariableValueInsertionTrie trieSegment)
            {
                newValue = RewriteObjectValueNode(
                    new ObjectValueNode(),
                    trieSegment,
                    routeData,
                    query);
            }
            else
            {
                throw new NotSupportedException();
            }

            newFields.Add(new ObjectFieldNode(fieldName, newValue));
        }

        return objectValueNode.WithFields(newFields);
    }

    private static readonly NullValueNode s_nullValueNode = new(null);

    private static bool TryGetValueForParameter(
        VariableValueInsertionTrieLeaf leaf,
        RouteData routeData,
        IQueryCollection query,
        [NotNullWhen(true)] out IValueNode? parameterValue)
    {
        parameterValue = null;

        if (leaf.ParameterType is OpenApiEndpointParameterType.Route)
        {
            if (!routeData.Values.TryGetValue(leaf.ParameterKey, out var value))
            {
                if (leaf.HasDefaultValue)
                {
                    return false;
                }

                parameterValue = s_nullValueNode;
                return true;
            }

            try
            {
                parameterValue = ParseValueNode(value, leaf.Type);
                return true;
            }
            catch (InvalidFormatException)
            {
                throw new BadRequestException($"Could not parse value for route parameter '{leaf.ParameterKey}'");
            }
        }

        if (leaf.ParameterType is OpenApiEndpointParameterType.Query)
        {
            if (!query.TryGetValue(leaf.ParameterKey, out var values) || values is not [{ } value])
            {
                if (leaf.HasDefaultValue)
                {
                    return false;
                }

                parameterValue = s_nullValueNode;
                return true;
            }

            try
            {
                parameterValue = ParseValueNode(value, leaf.Type);
                return true;
            }
            catch (InvalidFormatException)
            {
                throw new BadRequestException($"Could not parse value for query parameter '{leaf.ParameterKey}'");
            }
        }

        return false;
    }

    // TODO: Maybe we can optimize this further, so we don't have to perform a lot of checks at runtime
    private static IValueNode ParseValueNode(object? value, ITypeDefinition type)
    {
        if (value is null)
        {
            return s_nullValueNode;
        }

        if (type is IEnumTypeDefinition enumType)
        {
            if (value is not string s)
            {
                throw new InvalidFormatException("Expected a string value");
            }

            var matchingValue = enumType.Values.FirstOrDefault(v => v.Name == s);

            if (matchingValue is null)
            {
                throw new InvalidFormatException("Expected to find a matching enum value for string");
            }

            return new EnumValueNode(matchingValue.Name);
        }

        if (type is IScalarTypeDefinition scalarType)
        {
            var serializationType = scalarType.GetScalarSerializationType();

            if (serializationType.HasFlag(ScalarSerializationType.String))
            {
                if (value is string s)
                {
                    return new StringValueNode(s);
                }
            }

            if (serializationType.HasFlag(ScalarSerializationType.Int))
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

            if (serializationType.HasFlag(ScalarSerializationType.Boolean))
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

            if (serializationType.HasFlag(ScalarSerializationType.Float))
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

#if NET9_0_OR_GREATER
        return Results.InternalServerError();
#else
        return Results.StatusCode(500);
#endif
    }

    private class BadRequestException(string message) : Exception(message);
}
