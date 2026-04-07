using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;
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

        // Single request: use the simple path (no aliasing needed).
        if (requests.Length == 1)
        {
            var response = await ExecuteAsync(context, requests[0], cancellationToken)
                .ConfigureAwait(false);

            await foreach (var result in response.ReadAsResultStreamAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return new BatchStreamResult(0, result);
            }

            yield break;
        }

        // Rewrite each request and classify as entity lookup or passthrough.
        var rewrittenOps = new RewrittenOperation[requests.Length];
        var allEntityLookups = true;

        for (var i = 0; i < requests.Length; i++)
        {
            var rewritten = _queryRewriter.GetOrRewrite(
                requests[i].OperationSourceText,
                requests[i].OperationHash);
            rewrittenOps[i] = rewritten;

            if (!rewritten.IsEntityLookup)
            {
                allEntityLookups = false;
            }
        }

        // If any request is a passthrough, fall back to sequential execution
        // for all requests. Batching passthrough queries with entity lookups
        // requires variable namespace merging which is deferred for now.
        if (!allEntityLookups)
        {
            for (var i = 0; i < requests.Length; i++)
            {
                var response = await ExecuteAsync(context, requests[i], cancellationToken)
                    .ConfigureAwait(false);

                await foreach (var result in response.ReadAsResultStreamAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    yield return new BatchStreamResult(i, result);
                }
            }

            yield break;
        }

        // All requests are entity lookups: build one combined aliased query.
        await foreach (var batchResult in ExecuteBatchedEntityLookupsAsync(
            requests, rewrittenOps, cancellationToken).ConfigureAwait(false))
        {
            yield return batchResult;
        }
    }

    private async IAsyncEnumerable<BatchStreamResult> ExecuteBatchedEntityLookupsAsync(
        ImmutableArray<SourceSchemaClientRequest> requests,
        RewrittenOperation[] rewrittenOps,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 1. Build the combined query AST and variables JSON.
        var (combinedQueryText, combinedVariablesJson) =
            BuildCombinedEntityQuery(requests, rewrittenOps);

        // 2. Build the variable segment for the HTTP request.
        var variablesBytes = Encoding.UTF8.GetBytes(combinedVariablesJson);
        var buffer = new ChunkedArrayWriter();
        var span = buffer.GetSpan(variablesBytes.Length);
        variablesBytes.CopyTo(span);
        buffer.Advance(variablesBytes.Length);
        var variableSegment = JsonSegment.Create(buffer, 0, variablesBytes.Length);
        var variableValues = new VariableValues(CompactPath.Root, variableSegment);

        // 3. Send the combined request.
        var operationRequest = new OperationRequest(
            combinedQueryText,
            id: null,
            operationName: null,
            onError: null,
            variables: variableValues,
            extensions: JsonSegment.Empty);

        var httpRequest = new GraphQLHttpRequest(operationRequest);
        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        // 4. Parse the response and yield per-request results.
        SourceResultDocument? sourceDocument = null;

        try
        {
            sourceDocument = await httpResponse.ReadAsResultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!sourceDocument.Root.TryGetProperty("data"u8, out var dataElement)
                || dataElement.ValueKind != JsonValueKind.Object)
            {
                // No data in response: yield the raw result for the first request.
                var path = requests[0].Variables.IsDefaultOrEmpty
                    ? CompactPath.Root
                    : requests[0].Variables[0].Path;
                yield return new BatchStreamResult(0, new SourceSchemaResult(path, sourceDocument));
                sourceDocument = null; // ownership transferred
                yield break;
            }

            for (var i = 0; i < requests.Length; i++)
            {
                var aliasName = $"____request{i}";
                var aliasNameBytes = Encoding.UTF8.GetBytes(aliasName);
                var lookupFieldName = rewrittenOps[i].LookupFieldName!;
                var variables = requests[i].Variables;

                if (!dataElement.TryGetProperty(aliasNameBytes, out var aliasElement)
                    || aliasElement.ValueKind != JsonValueKind.Array)
                {
                    // Alias not found or not an array: yield an empty-data result.
                    var emptyJson = $"{{\"data\":{{\"{lookupFieldName}\":null}}}}";
                    var emptyBytes = Encoding.UTF8.GetBytes(emptyJson);
                    var emptyDoc = SourceResultDocument.Parse(emptyBytes, emptyBytes.Length);

                    var path = variables.IsDefaultOrEmpty
                        ? CompactPath.Root
                        : variables[0].Path;
                    yield return new BatchStreamResult(i, new SourceSchemaResult(path, emptyDoc));
                    continue;
                }

                var entityCount = aliasElement.GetArrayLength();

                for (var j = 0; j < entityCount; j++)
                {
                    var entity = aliasElement[j];
                    var entityJson = BuildWrappedEntityJson(lookupFieldName, entity);
                    var entityBytes = Encoding.UTF8.GetBytes(entityJson);
                    var entityDocument = SourceResultDocument.Parse(entityBytes, entityBytes.Length);

                    CompactPath resultPath;
                    ImmutableArray<CompactPath> additionalPaths;

                    if (variables.IsDefaultOrEmpty || j >= variables.Length)
                    {
                        resultPath = CompactPath.Root;
                        additionalPaths = [];
                    }
                    else
                    {
                        resultPath = variables[j].Path;
                        additionalPaths = variables[j].AdditionalPaths;
                    }

                    yield return additionalPaths.IsDefaultOrEmpty
                        ? new BatchStreamResult(i, new SourceSchemaResult(resultPath, entityDocument))
                        : new BatchStreamResult(i, new SourceSchemaResult(resultPath, entityDocument, additionalPaths: additionalPaths));
                }
            }
        }
        finally
        {
            sourceDocument?.Dispose();
            httpResponse.Dispose();
            buffer.Dispose();
        }
    }

    /// <summary>
    /// Builds a combined aliased <c>_entities</c> query and variables JSON from
    /// multiple entity lookup requests. Each request gets a unique alias
    /// (<c>____request0</c>, <c>____request1</c>, ...) and a unique variable
    /// name (<c>$r0</c>, <c>$r1</c>, ...).
    /// </summary>
    internal static (string QueryText, string VariablesJson) BuildCombinedEntityQuery(
        ImmutableArray<SourceSchemaClientRequest> requests,
        RewrittenOperation[] rewrittenOps)
    {
        var variableDefinitions = new List<VariableDefinitionNode>(requests.Length);
        var fieldNodes = new List<ISelectionNode>(requests.Length);

        for (var i = 0; i < requests.Length; i++)
        {
            var rewritten = rewrittenOps[i];
            var varName = $"r{i}";
            var aliasName = $"____request{i}";

            // Variable definition: $r{i}: [_Any!]!
            variableDefinitions.Add(new VariableDefinitionNode(
                location: null,
                new VariableNode(varName),
                description: null,
                type: new NonNullTypeNode(
                    new ListTypeNode(
                        new NonNullTypeNode(
                            new NamedTypeNode("_Any")))),
                defaultValue: null,
                directives: []));

            // Field: ____request{i}: _entities(representations: $r{i}) { ... on EntityType { ... } }
            var inlineFragment = rewritten.InlineFragment
                ?? new InlineFragmentNode(
                    location: null,
                    typeCondition: new NamedTypeNode(rewritten.EntityTypeName!),
                    directives: [],
                    selectionSet: new SelectionSetNode(Array.Empty<ISelectionNode>()));

            fieldNodes.Add(new FieldNode(
                location: null,
                new NameNode("_entities"),
                alias: new NameNode(aliasName),
                directives: [],
                arguments: [new ArgumentNode("representations", new VariableNode(varName))],
                selectionSet: new SelectionSetNode([inlineFragment])));
        }

        var operation = new OperationDefinitionNode(
            location: null,
            name: null,
            description: null,
            operation: OperationType.Query,
            variableDefinitions: variableDefinitions,
            directives: [],
            selectionSet: new SelectionSetNode(fieldNodes));

        var document = new DocumentNode([operation]);
        var queryText = document.ToString(indented: true);

        // Build the combined variables JSON.
        var variablesJson = BuildCombinedVariablesJson(requests, rewrittenOps);

        return (queryText, variablesJson);
    }

    /// <summary>
    /// Builds the combined variables JSON object for a batched entity query.
    /// Each request's representations are written under a <c>r{i}</c> key.
    /// </summary>
    private static string BuildCombinedVariablesJson(
        ImmutableArray<SourceSchemaClientRequest> requests,
        RewrittenOperation[] rewrittenOps)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();

        for (var i = 0; i < requests.Length; i++)
        {
            var varName = $"r{i}";
            var rewritten = rewrittenOps[i];

            writer.WritePropertyName(varName);

            // Write the representations array for this request.
            WriteRepresentationsArray(
                writer,
                requests[i].Variables,
                rewritten.EntityTypeName!,
                rewritten.VariableToKeyFieldMap);
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Writes a JSON array of entity representations for the given variable sets.
    /// Extracted from <see cref="BuildRepresentationsJson"/> to allow reuse
    /// in the combined variables builder.
    /// </summary>
    private static void WriteRepresentationsArray(
        Utf8JsonWriter writer,
        ImmutableArray<VariableValues> variableSets,
        string entityTypeName,
        IReadOnlyDictionary<string, string> variableToKeyFieldMap)
    {
        writer.WriteStartArray();

        if (variableSets.IsDefaultOrEmpty)
        {
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
                    var sequence = values.AsSequence();
                    var reader = new Utf8JsonReader(sequence);

                    if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                    {
                        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                        {
                            var propertyName = reader.GetString()!;
                            reader.Read();

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
    }

    /// <summary>
    /// Builds a JSON wrapper for a single entity result:
    /// <c>{"data": {"&lt;fieldName&gt;": &lt;entity&gt;}}</c>.
    /// </summary>
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

        WriteRepresentationsArray(writer, variableSets, entityTypeName, variableToKeyFieldMap);

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
                var entityJson = ApolloFederationSourceSchemaClient.BuildWrappedEntityJson(
                    lookupFieldName, entity);
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

        public override void Dispose()
        {
            response.Dispose();
            buffer?.Dispose();
        }
    }
}
