using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// An <see cref="ISourceSchemaClient"/> implementation that translates Fusion's
/// composite-schema-spec queries into Apollo Federation <c>_entities</c> queries
/// and sends them to an Apollo subgraph over HTTP.
/// </summary>
public sealed class ApolloFederationSourceSchemaClient : ISourceSchemaClient
{
    private static readonly Uri s_unknownUri = new("http://unknown");

    private readonly GraphQLHttpClient _httpClient;
    private readonly FederationQueryRewriter _queryRewriter;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="ApolloFederationSourceSchemaClient"/>.
    /// </summary>
    /// <param name="httpClient">The underlying GraphQL HTTP client.</param>
    /// <param name="queryRewriter">The query rewriter for this source schema.</param>
    internal ApolloFederationSourceSchemaClient(
        GraphQLHttpClient httpClient,
        FederationQueryRewriter queryRewriter)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(queryRewriter);

        _httpClient = httpClient;
        _queryRewriter = queryRewriter;
    }

    /// <inheritdoc />
    public SourceSchemaClientCapabilities Capabilities
        => SourceSchemaClientCapabilities.VariableBatching
            | SourceSchemaClientCapabilities.RequestBatching;

    /// <inheritdoc />
    public async ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rewritten = _queryRewriter.GetOrRewrite(
            request.OperationSourceText,
            request.OperationHash);

        if (!rewritten.IsEntityLookup)
        {
            return await ExecutePassthroughAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        return await ExecuteEntityLookupAsync(request, rewritten, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<BatchStreamResult> ExecuteBatchStreamAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        for (var i = 0; i < requests.Length; i++)
        {
            var request = requests[i];
            var response = await ExecuteAsync(context, request, cancellationToken)
                .ConfigureAwait(false);

            await foreach (var result in response.ReadAsResultStreamAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return new BatchStreamResult(i, result);
            }
        }
    }

    private async ValueTask<SourceSchemaClientResponse> ExecutePassthroughAsync(
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        var operationRequest = new OperationRequest(
            request.OperationSourceText,
            id: null,
            operationName: null,
            onError: null,
            variables: request.Variables.IsDefaultOrEmpty
                ? VariableValues.Empty
                : request.Variables[0],
            extensions: JsonSegment.Empty);

        var httpRequest = new GraphQLHttpRequest(operationRequest);
        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        return new PassthroughResponse(
            httpRequest.Uri ?? s_unknownUri,
            request.Variables,
            httpResponse);
    }

    private async ValueTask<SourceSchemaClientResponse> ExecuteEntityLookupAsync(
        SourceSchemaClientRequest request,
        RewrittenOperation rewritten,
        CancellationToken cancellationToken)
    {
        // Build the representations JSON and send as a single _entities query.
        var representationsJson = BuildRepresentationsJson(
            request.Variables,
            rewritten.EntityTypeName!,
            rewritten.VariableToKeyFieldMap);

        // Build the variable JSON: {"representations": [...]}
        var variablesJson = $"{{\"representations\":{representationsJson}}}";
        var variablesBytes = Encoding.UTF8.GetBytes(variablesJson);

        var buffer = new ChunkedArrayWriter();
        var span = buffer.GetSpan(variablesBytes.Length);
        variablesBytes.CopyTo(span);
        buffer.Advance(variablesBytes.Length);
        var variableSegment = JsonSegment.Create(buffer, 0, variablesBytes.Length);

        var variableValues = new VariableValues(CompactPath.Root, variableSegment);

        var operationRequest = new OperationRequest(
            rewritten.OperationText,
            id: null,
            operationName: null,
            onError: null,
            variables: variableValues,
            extensions: JsonSegment.Empty);

        var httpRequest = new GraphQLHttpRequest(operationRequest);
        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        return new EntityLookupResponse(
            httpRequest.Uri ?? s_unknownUri,
            request.Variables,
            rewritten.LookupFieldName!,
            httpResponse,
            buffer);
    }

    /// <summary>
    /// Builds the JSON array of representations for the <c>_entities</c> query.
    /// Each representation is: <c>{"__typename": "Product", "id": &lt;value&gt;, ...}</c>
    /// </summary>
    private static string BuildRepresentationsJson(
        ImmutableArray<VariableValues> variableSets,
        string entityTypeName,
        IReadOnlyDictionary<string, string> variableToKeyFieldMap)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartArray();

        if (variableSets.IsDefaultOrEmpty)
        {
            // Single empty representation with just __typename.
            writer.WriteStartObject();
            writer.WriteString("__typename", entityTypeName);
            writer.WriteEndObject();
        }
        else
        {
            for (var i = 0; i < variableSets.Length; i++)
            {
                writer.WriteStartObject();
                writer.WriteString("__typename", entityTypeName);

                var values = variableSets[i].Values;

                if (!values.IsEmpty)
                {
                    // Parse the variable values JSON to extract key fields.
                    var sequence = values.AsSequence();
                    var reader = new Utf8JsonReader(sequence);

                    if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                    {
                        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                        {
                            var propertyName = reader.GetString()!;
                            reader.Read(); // advance to value

                            if (variableToKeyFieldMap.TryGetValue(propertyName, out var keyFieldName))
                            {
                                writer.WritePropertyName(keyFieldName);
                                WriteCurrentValue(writer, ref reader);
                            }
                            else
                            {
                                reader.Skip();
                            }
                        }
                    }
                }

                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCurrentValue(Utf8JsonWriter writer, ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                writer.WriteStringValue(reader.GetString());
                break;

            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var longValue))
                {
                    writer.WriteNumberValue(longValue);
                }
                else
                {
                    writer.WriteNumberValue(reader.GetDouble());
                }
                break;

            case JsonTokenType.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonTokenType.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonTokenType.Null:
                writer.WriteNullValue();
                break;

            case JsonTokenType.StartObject:
            case JsonTokenType.StartArray:
                // Write complex values using the built-in copy mechanism.
                WriteComplexValue(writer, ref reader);
                break;
        }
    }

    private static void WriteComplexValue(Utf8JsonWriter writer, ref Utf8JsonReader reader)
    {
        var depth = reader.CurrentDepth;
        var isArray = reader.TokenType == JsonTokenType.StartArray;

        if (isArray)
        {
            writer.WriteStartArray();
        }
        else
        {
            writer.WriteStartObject();
        }

        while (reader.Read())
        {
            if (reader.CurrentDepth == depth)
            {
                if (isArray)
                {
                    writer.WriteEndArray();
                }
                else
                {
                    writer.WriteEndObject();
                }
                return;
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    writer.WritePropertyName(reader.GetString()!);
                    break;

                case JsonTokenType.String:
                    writer.WriteStringValue(reader.GetString());
                    break;

                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var l))
                    {
                        writer.WriteNumberValue(l);
                    }
                    else
                    {
                        writer.WriteNumberValue(reader.GetDouble());
                    }
                    break;

                case JsonTokenType.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonTokenType.False:
                    writer.WriteBooleanValue(false);
                    break;

                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    break;

                case JsonTokenType.StartObject:
                    writer.WriteStartObject();
                    break;

                case JsonTokenType.StartArray:
                    writer.WriteStartArray();
                    break;

                case JsonTokenType.EndObject:
                    writer.WriteEndObject();
                    break;

                case JsonTokenType.EndArray:
                    writer.WriteEndArray();
                    break;
            }
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _httpClient.Dispose();
        _disposed = true;

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Response for passthrough (non-entity-lookup) queries.
    /// Delegates directly to the underlying HTTP response.
    /// </summary>
    private sealed class PassthroughResponse(
        Uri uri,
        ImmutableArray<VariableValues> variables,
        GraphQLHttpResponse response)
        : SourceSchemaClientResponse
    {
        public override Uri Uri => uri;

        public override string ContentType => response.RawContentType ?? "unknown";

        public override bool IsSuccessful => response.IsSuccessStatusCode;

        public override async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var result = await response.ReadAsResultAsync(cancellationToken).ConfigureAwait(false);

            if (variables.IsDefaultOrEmpty || variables.Length <= 1)
            {
                var path = variables.IsDefaultOrEmpty
                    ? CompactPath.Root
                    : variables[0].Path;
                var additionalPaths = variables.IsDefaultOrEmpty
                    ? ImmutableArray<CompactPath>.Empty
                    : variables[0].AdditionalPaths;

                yield return additionalPaths.IsDefaultOrEmpty
                    ? new SourceSchemaResult(path, result)
                    : new SourceSchemaResult(path, result, additionalPaths: additionalPaths);
            }
            else
            {
                for (var i = 0; i < variables.Length; i++)
                {
                    var variable = variables[i];
                    yield return variable.AdditionalPaths.IsDefaultOrEmpty
                        ? new SourceSchemaResult(variable.Path, result)
                        : new SourceSchemaResult(variable.Path, result, additionalPaths: variable.AdditionalPaths);
                }
            }
        }

        public override void Dispose() => response.Dispose();
    }

    /// <summary>
    /// Response for entity lookup queries. Reads the <c>_entities</c> array from the
    /// subgraph response and yields one <see cref="SourceSchemaResult"/> per entity,
    /// wrapping each entity as if it were the direct result of the lookup field.
    /// </summary>
    private sealed class EntityLookupResponse(
        Uri uri,
        ImmutableArray<VariableValues> variables,
        string lookupFieldName,
        GraphQLHttpResponse response,
        ChunkedArrayWriter? buffer)
        : SourceSchemaClientResponse
    {
        public override Uri Uri => uri;

        public override string ContentType => response.RawContentType ?? "unknown";

        public override bool IsSuccessful => response.IsSuccessStatusCode;

        public override async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var sourceDocument = await response.ReadAsResultAsync(cancellationToken)
                .ConfigureAwait(false);

            // The subgraph response looks like:
            // {"data": {"_entities": [{"id":"1","name":"Widget"}, ...]}}
            //
            // We need to yield per-entity results that look like:
            // {"data": {"productById": {"id":"1","name":"Widget"}}}
            //
            // For each entity in the _entities array, we build a wrapper document.

            if (!sourceDocument.Root.TryGetProperty("data"u8, out var dataElement)
                || dataElement.ValueKind != JsonValueKind.Object)
            {
                // If there's no data or an error, yield the raw result.
                var path = variables.IsDefaultOrEmpty ? CompactPath.Root : variables[0].Path;
                yield return new SourceSchemaResult(path, sourceDocument);
                yield break;
            }

            if (!dataElement.TryGetProperty("_entities"u8, out var entitiesElement)
                || entitiesElement.ValueKind != JsonValueKind.Array)
            {
                // No _entities array — yield raw result.
                var path = variables.IsDefaultOrEmpty ? CompactPath.Root : variables[0].Path;
                yield return new SourceSchemaResult(path, sourceDocument);
                yield break;
            }

            var entityCount = entitiesElement.GetArrayLength();

            for (var i = 0; i < entityCount; i++)
            {
                var entity = entitiesElement[i];

                // Build a wrapper: {"data": {"<lookupFieldName>": <entity>}}
                var entityJson = BuildWrappedEntityJson(lookupFieldName, entity);
                var entityBytes = Encoding.UTF8.GetBytes(entityJson);
                var entityDocument = SourceResultDocument.Parse(entityBytes, entityBytes.Length);

                CompactPath resultPath;
                ImmutableArray<CompactPath> additionalPaths;

                if (variables.IsDefaultOrEmpty || i >= variables.Length)
                {
                    resultPath = CompactPath.Root;
                    additionalPaths = [];
                }
                else
                {
                    resultPath = variables[i].Path;
                    additionalPaths = variables[i].AdditionalPaths;
                }

                yield return additionalPaths.IsDefaultOrEmpty
                    ? new SourceSchemaResult(resultPath, entityDocument)
                    : new SourceSchemaResult(resultPath, entityDocument, additionalPaths: additionalPaths);
            }

            sourceDocument.Dispose();
        }

        private static string BuildWrappedEntityJson(string fieldName, SourceResultElement entity)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();
            writer.WritePropertyName("data");
            writer.WriteStartObject();
            writer.WritePropertyName(fieldName);
            WriteSourceResultElement(writer, entity);
            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static void WriteSourceResultElement(Utf8JsonWriter writer, SourceResultElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        WriteSourceResultElement(writer, property.Value);
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        WriteSourceResultElement(writer, item);
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    writer.WriteStringValue(element.GetString());
                    break;

                case JsonValueKind.Number:
                    writer.WriteRawValue(element.GetRawText());
                    break;

                case JsonValueKind.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonValueKind.False:
                    writer.WriteBooleanValue(false);
                    break;

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default:
                    writer.WriteNullValue();
                    break;
            }
        }

        public override void Dispose()
        {
            response.Dispose();
            buffer?.Dispose();
        }
    }
}
