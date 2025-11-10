using System.Collections.Immutable;
using HotChocolate.AspNetCore;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

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
        catch (InvalidFormatException)
        {
            await Results.BadRequest().ExecuteAsync(context);
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

        // Group parameters by variable name and build tries for efficient path insertion
        // Only parameters with InputObjectPath need trie processing
        var variableTries = new Dictionary<string, VariableValueTrie>();

        RouteData? routeData = null;

        foreach (var parameter in endpointDescriptor.Parameters)
        {
            IValueNode? coercedValue;

            if (parameter.ParameterType is OpenApiEndpointParameterType.Route)
            {
                routeData ??= httpContext.GetRouteData();

                if (!routeData.Values.TryGetValue(parameter.ParameterKey, out var value))
                {
                    // We just skip here and let the GraphQL execution take care of the validation
                    continue;
                }

                coercedValue = ParseValueNode(value, parameter.Type);
            }
            else if (parameter.ParameterType is OpenApiEndpointParameterType.Query)
            {
                if (!httpContext.Request.Query.TryGetValue(parameter.ParameterKey, out var values)
                    || values is not [{} value])
                {
                    // We just skip here and let the GraphQL execution take care of the validation
                    continue;
                }

                coercedValue = ParseValueNode(value, parameter.Type);
            }
            else
            {
                throw new NotSupportedException();
            }

            if (coercedValue is null)
            {
                // Skip if we couldn't parse the value - GraphQL execution will handle validation
                continue;
            }

            // Direct variable assignment (no path) - assign directly without trie
            if (parameter.InputObjectPath is not { Length: > 0 })
            {
                variables[parameter.VariableName] = coercedValue;
                continue;
            }

            // Path-based assignment - use trie for efficient handling of overlapping paths
            if (!variableTries.TryGetValue(parameter.VariableName, out var trie))
            {
                trie = new VariableValueTrie();
                variableTries[parameter.VariableName] = trie;
            }

            trie.Add(parameter.InputObjectPath, coercedValue);
        }

        // Build variable values from tries, handling overlapping paths efficiently
        foreach (var (variableName, trie) in variableTries)
        {
            var trieValue = trie.BuildValueNode();

            // If this variable was already set (e.g., from body or direct assignment), merge the values
            if (variables.TryGetValue(variableName, out var existingValue) && existingValue is ObjectValueNode existingObject)
            {
                // Merge trie-built value into existing object at the root level
                variables[variableName] = MergeObjectValues(existingObject, trieValue);
            }
            else if (trieValue is not null)
            {
                variables[variableName] = trieValue;
            }
        }

        return variables;
    }

    // TODO: Maybe we can optimize this further, so we don't have to perform a lot of checks at runtime
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

        return Results.InternalServerError();
    }

    /// <summary>
    /// Merges a trie-built value into an existing object value.
    /// </summary>
    private static ObjectValueNode MergeObjectValues(ObjectValueNode existing, IValueNode? trieValue)
    {
        if (trieValue is not ObjectValueNode trieObject)
        {
            return existing;
        }

        var mergedFields = new List<ObjectFieldNode>(existing.Fields);
        var existingKeys = new HashSet<string>(existing.Fields.Select(f => f.Name.Value));

        foreach (var field in trieObject.Fields)
        {
            if (existingKeys.Contains(field.Name.Value))
            {
                // Merge nested objects if both are objects, otherwise replace
                var existingField = mergedFields.First(f => f.Name.Value == field.Name.Value);
                var mergedValue = existingField.Value is ObjectValueNode existingFieldObject
                    && field.Value is ObjectValueNode fieldObject
                    ? MergeObjectValues(existingFieldObject, fieldObject)
                    : field.Value;

                var index = mergedFields.FindIndex(f => f.Name.Value == field.Name.Value);
                mergedFields[index] = new ObjectFieldNode(field.Name.Value, mergedValue);
            }
            else
            {
                // Add new field
                mergedFields.Add(field);
            }
        }

        return new ObjectValueNode(mergedFields);
    }

    /// <summary>
    /// A trie structure for efficiently building IValueNode hierarchies from overlapping paths.
    /// </summary>
    private sealed class VariableValueTrie : Dictionary<string, VariableValueTrie>
    {
        public IValueNode? TerminalValue { get; private set; }

        /// <summary>
        /// Adds a value at the specified path.
        /// </summary>
        public void Add(ImmutableArray<string>? path, IValueNode value)
        {
            if (path is not { Length: > 0 } inputObjectPath)
            {
                // Direct variable assignment (no path)
                TerminalValue = value;
                return;
            }

            var currentNode = this;

            foreach (var segment in inputObjectPath)
            {
                if (!currentNode.TryGetValue(segment, out var nextNode))
                {
                    nextNode = new VariableValueTrie();
                    currentNode[segment] = nextNode;
                }

                currentNode = nextNode;
            }

            currentNode.TerminalValue = value;
        }

        /// <summary>
        /// Builds the IValueNode hierarchy from the trie structure.
        /// </summary>
        public IValueNode? BuildValueNode()
        {
            if (TerminalValue is not null && Count == 0)
            {
                // Leaf node with direct value
                return TerminalValue;
            }

            if (Count == 0)
            {
                // Empty node
                return TerminalValue;
            }

            // Build object from child nodes
            var fields = new List<ObjectFieldNode>(Count);

            foreach (var (key, childTrie) in this)
            {
                var childValue = childTrie.BuildValueNode();

                if (childValue is not null)
                {
                    fields.Add(new ObjectFieldNode(key, childValue));
                }
            }

            if (fields.Count == 0)
            {
                return TerminalValue;
            }

            var objectValue = new ObjectValueNode(fields);

            // If we also have a terminal value, we need to merge it
            // This shouldn't happen in normal cases, but handle it gracefully
            if (TerminalValue is ObjectValueNode terminalObject)
            {
                // Merge terminal object fields with trie fields (trie takes precedence)
                var mergedFields = new List<ObjectFieldNode>(terminalObject.Fields);
                var existingKeys = new HashSet<string>(fields.Select(f => f.Name.Value));

                foreach (var field in terminalObject.Fields)
                {
                    if (!existingKeys.Contains(field.Name.Value))
                    {
                        mergedFields.Add(field);
                    }
                }

                return new ObjectValueNode(mergedFields);
            }

            return objectValue;
        }
    }
}
