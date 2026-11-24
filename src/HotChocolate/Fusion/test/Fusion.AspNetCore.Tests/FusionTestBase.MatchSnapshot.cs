using System.Buffers;
using System.Collections.Frozen;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CookieCrumble.HotChocolate.Formatters;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Policies;
using HotChocolate.Language;
using HotChocolate.Text.Yaml;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;
using OperationResult = HotChocolate.Transport.OperationResult;

namespace HotChocolate.Fusion;

public abstract partial class FusionTestBase
{
    protected Task MatchSnapshotAsync(
        Gateway gateway,
        OperationRequest request,
        GraphQLHttpResponse response,
        string? postFix = null,
        RawRequest? rawRequest = null,
        bool stableStream = false)
        => MatchSnapshotCoreAsync(
            gateway,
            request,
            response,
            assertResults: null,
            postFix,
            rawRequest,
            stableStream);

    protected Task AssertAndMatchSnapshotAsync(
        Gateway gateway,
        OperationRequest request,
        GraphQLHttpResponse response,
        Action<IReadOnlyList<OperationResult>> assertResults)
    {
        ArgumentNullException.ThrowIfNull(assertResults);

        return MatchSnapshotCoreAsync(
            gateway,
            request,
            response,
            assertResults,
            postFix: null,
            rawRequest: null,
            stableStream: false);
    }

    protected Task AssertAndMatchSnapshotAsync(
        Gateway gateway,
        OperationRequest request,
        GraphQLHttpResponse response,
        Action<IReadOnlyList<OperationResult>> assertResults,
        string postFix)
    {
        ArgumentNullException.ThrowIfNull(assertResults);

        return MatchSnapshotCoreAsync(
            gateway,
            request,
            response,
            assertResults,
            postFix,
            rawRequest: null,
            stableStream: false);
    }

    private async Task MatchSnapshotCoreAsync(
        Gateway gateway,
        OperationRequest request,
        GraphQLHttpResponse response,
        Action<IReadOnlyList<OperationResult>>? assertResults,
        string? postFix,
        RawRequest? rawRequest,
        bool stableStream)
    {
        var snapshot = new Snapshot(postFix, ".yaml");

        var results = new List<OperationResult>();
        string? stableStreamText = null;

        if (stableStream)
        {
            var bodyBytes = await response.HttpResponseMessage.Content.ReadAsByteArrayAsync();

            using var formatterResponseMessage = CloneHttpResponseMessage(response.HttpResponseMessage, bodyBytes);
            using var formatterResponse = new GraphQLHttpResponse(formatterResponseMessage);

            var buffer = new ArrayBufferWriter<byte>();
            SnapshotValueFormatters.GraphQLHttpStable.Format(buffer, formatterResponse);
            stableStreamText = Encoding.UTF8.GetString(buffer.WrittenSpan);

            using var resultsResponseMessage = CloneHttpResponseMessage(response.HttpResponseMessage, bodyBytes);
            using var resultsResponse = new GraphQLHttpResponse(resultsResponseMessage);

            await foreach (var result in resultsResponse.ReadAsResultStreamAsync())
            {
                results.Add(result);
            }
        }
        else
        {
            // We first wait and capture all possible gateway responses.
            await foreach (var result in response.ReadAsResultStreamAsync())
            {
                results.Add(result);
            }
        }

        assertResults?.Invoke(results);

        var testServerRegistrations = gateway.Services
            .GetServices<TestServerRegistration>()
            .ToArray();

        using var snapshotBuffer = new PooledArrayWriter();
        var writer = new YamlWriter(snapshotBuffer);
        writer.WriteStartObject();

        writer.WritePropertyName("title");
        writer.WriteStringValue(snapshot.Title);

        writer.WritePropertyName("request");
        writer.WriteStartObject();
        WriteOperationRequest(writer, request, rawRequest);
        writer.WriteEndObject();

        WriteResults(writer, results, stableStreamText);

        writer.WritePropertyName("sourceSchemas");
        writer.WriteStartArray();

        foreach (var sourceSchema in gateway.SourceSchemas)
        {
            WriteSourceSchema(writer, gateway, testServerRegistrations, sourceSchema);
        }

        writer.WriteEndArray();

        await TryWriteOperationPlanAsync(writer, gateway, results);

        writer.WriteEndObject();

        // Strip source file line numbers from stack traces so that line shifts
        // in production code do not cause snapshot mismatches.
        var snapshotText = StackTraceLineRegex().Replace(
            Encoding.UTF8.GetString(snapshotBuffer.WrittenSpan),
            " in $1");
        snapshot.Add(snapshotText);

        foreach (var result in results)
        {
            result.Dispose();
        }

        gateway.Interactions.Clear();

        await snapshot.MatchAsync();
    }

    private static async Task TryWriteOperationPlanAsync(
        YamlWriter writer,
        Gateway gateway,
        List<OperationResult> results)
    {
        foreach (var result in results)
        {
            if (result.Extensions.ValueKind is JsonValueKind.Object
                && result.Extensions.TryGetProperty("fusion", out var fusionProperty)
                && fusionProperty.TryGetProperty("operationPlan", out var operationPlanProperty))
            {
                var manager = gateway.Services.GetRequiredService<FusionRequestExecutorManager>();
                var executor = await manager.GetExecutorAsync();
                var operationCompiler = executor.Schema.Services.GetRequiredService<OperationCompiler>();
                var parser = new JsonOperationPlanParser(
                    operationCompiler,
                    executor.Schema.Services.GetRequiredService<IPolicyResolver>());

                var buffer = new PooledArrayWriter();
                await using var jsonWriter = new Utf8JsonWriter(buffer);

                operationPlanProperty.WriteTo(jsonWriter);
                await jsonWriter.FlushAsync();

                var plan = parser.Parse(buffer.WrittenMemory);

                writer.WritePropertyName("operationPlan");
                YamlOperationPlanFormatter.Format(writer, plan, trace: null);

                break;
            }
        }
    }

    private void WriteResults(YamlWriter writer, List<OperationResult> results, string? stableStreamText = null)
    {
        if (stableStreamText is not null)
        {
            writer.WritePropertyName("stableResponseStream");
            WriteLiteral(writer, stableStreamText.TrimEnd());
            return;
        }

        if (results is [{ } singleResult])
        {
            writer.WritePropertyName("response");
            writer.WriteStartObject();
            writer.WritePropertyName("body");
            WriteResult(writer, singleResult);
            writer.WriteEndObject();
        }
        else
        {
            writer.WritePropertyName("responseStream");
            writer.WriteStartArray();

            foreach (var result in results)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("body");
                WriteResult(writer, result);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }

    private static HttpResponseMessage CloneHttpResponseMessage(HttpResponseMessage source, byte[] bodyBytes)
    {
        var clone = new HttpResponseMessage(source.StatusCode);

        foreach (var header in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var content = new ByteArrayContent(bodyBytes);

        if (source.Content.Headers.ContentType is not null)
        {
            content.Headers.ContentType =
                MediaTypeHeaderValue.Parse(source.Content.Headers.ContentType.ToString());
        }

        foreach (var header in source.Content.Headers)
        {
            if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)
                || string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        clone.Content = content;

        return clone;
    }

    private static void WriteResult(YamlWriter writer, OperationResult result)
    {
        var memoryStream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

        jsonWriter.WriteStartObject();

        if (result.RequestIndex.HasValue)
        {
            jsonWriter.WriteNumber("requestIndex", result.RequestIndex.Value);
        }

        if (result.VariableIndex.HasValue)
        {
            jsonWriter.WriteNumber("variableIndex", result.VariableIndex.Value);
        }

        if (result.Data.ValueKind is JsonValueKind.Object)
        {
            jsonWriter.WritePropertyName("data");
            result.Data.WriteTo(jsonWriter);
        }

        if (result.Errors.ValueKind is JsonValueKind.Array)
        {
            jsonWriter.WritePropertyName("errors");
            result.Errors.WriteTo(jsonWriter);
        }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        WriteLiteral(writer, Encoding.UTF8.GetString(memoryStream.ToArray()));
    }

    private static void WriteSourceSchema(
        YamlWriter writer,
        Gateway gateway,
        TestServerRegistration[] testServerRegistrations,
        SourceSchemaText sourceSchemaText)
    {
        var sourceSchemaName = sourceSchemaText.Name;
        var testServer = testServerRegistrations.First(r => r.Name == sourceSchemaName);

        writer.WriteStartObject();

        writer.WritePropertyName("name");
        writer.WriteStringValue(sourceSchemaName);

        writer.WritePropertyName("schema");
        WriteSourceSchemaDocument(writer, sourceSchemaText.SourceText);

        if (testServer.Options.IsTimingOut)
        {
            writer.WritePropertyName("isTimingOut");
            writer.WriteBooleanValue(true);
        }

        var interactions = gateway.Interactions.GetValueOrDefault(sourceSchemaName);

        if (interactions is not null)
        {
            writer.WritePropertyName("interactions");
            writer.WriteStartArray();

            // Order by (OperationPlanId, NodeId) so parallel mini-plans (e.g.
            // deferred groups) render in a stable order independent of the
            // runtime arrival order of their subgraph responses.
            foreach (var interaction in interactions.Values
                .OrderBy(x => x.OperationPlanId, StringComparer.Ordinal)
                .ThenBy(x => x.NodeId))
            {
                var request = interaction.Request!;

                writer.WriteStartObject();
                writer.WritePropertyName("request");
                writer.WriteStartObject();

                if (!string.IsNullOrEmpty(request.Accept))
                {
                    writer.WritePropertyName("accept");
                    writer.WriteStringValue(request.Accept);
                }

                if (request.ContentType.MediaType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) == true)
                {
                    WriteMultipartRequest(writer, request.ContentType, request.Body);
                }
                else
                {
                    var jsonBody = JsonDocument.Parse(request.Body);

                    if (jsonBody.RootElement.ValueKind is JsonValueKind.Array)
                    {
                        writer.WritePropertyName("kind");
                        writer.WriteStringValue("OperationBatch");
                        writer.WritePropertyName("items");
                        writer.WriteStartArray();

                        foreach (var item in jsonBody.RootElement.EnumerateArray())
                        {
                            writer.WriteStartObject();

                            item.TryGetProperty("query", out var queryProperty);
                            item.TryGetProperty("variables", out var variablesProperty);

                            WriteDocument(writer, queryProperty.GetString()!);

                            if (variablesProperty.ValueKind != JsonValueKind.Undefined)
                            {
                                writer.WritePropertyName("variables");
                                WriteFormattedJson(writer, variablesProperty);
                            }

                            TryWriteOnError(writer, item);

                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                    }
                    else
                    {
                        TryWriteOnError(writer, jsonBody.RootElement);

                        jsonBody.RootElement.TryGetProperty("query", out var queryProperty);
                        jsonBody.RootElement.TryGetProperty("variables", out var variablesProperty);

                        WriteDocument(writer, queryProperty.GetString()!);

                        if (variablesProperty.ValueKind != JsonValueKind.Undefined)
                        {
                            writer.WritePropertyName("variables");
                            WriteFormattedJson(writer, variablesProperty);
                        }
                    }
                }

                writer.WriteEndObject();

                if (interaction.StatusCode.HasValue)
                {
                    writer.WritePropertyName("response");
                    writer.WriteStartObject();

                    if (interaction.StatusCode != HttpStatusCode.OK)
                    {
                        writer.WritePropertyName("statusCode");
                        writer.WriteNumberValue((int)interaction.StatusCode);
                    }

                    if (!string.IsNullOrEmpty(interaction.ContentType)
                        && interaction.ContentType != "application/graphql-response+json; charset=utf-8")
                    {
                        writer.WritePropertyName("contentType");
                        writer.WriteStringValue(interaction.ContentType);
                    }

                    if (interaction.Results.Count > 0)
                    {
                        writer.WritePropertyName("results");
                        writer.WriteStartArray();

                        foreach (var result in interaction.Results)
                        {
                            WriteLiteral(writer, result);
                        }

                        writer.WriteEndArray();
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static string SerializeSourceSchemaResult(SourceSchemaResult result)
    {
        var memoryStream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

        jsonWriter.WriteStartObject();

        if (result.Errors is { } errors)
        {
            jsonWriter.WritePropertyName("errors");
            WriteSourceSchemaErrors(jsonWriter, errors);
        }

        if (result.Data.ValueKind != JsonValueKind.Undefined)
        {
            jsonWriter.WritePropertyName("data");
            jsonWriter.WriteRawValue(result.Data.GetRawValue());
        }

        if (result.Extensions.ValueKind != JsonValueKind.Undefined)
        {
            jsonWriter.WritePropertyName("extensions");
            jsonWriter.WriteRawValue(result.Extensions.GetRawValue());
        }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        memoryStream.Position = 0;

        using var document = JsonDocument.Parse(memoryStream);
        return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Writes the errors a source schema result carries, the errors without a path first and the
    /// errors with a path in the order of the paths they were reported for.
    /// </summary>
    private static void WriteSourceSchemaErrors(Utf8JsonWriter jsonWriter, SourceSchemaErrors errors)
    {
        jsonWriter.WriteStartArray();

        foreach (var error in errors.RootErrors)
        {
            WriteSourceSchemaError(jsonWriter, error);
        }

        WriteSourceSchemaErrors(jsonWriter, errors.Trie);

        jsonWriter.WriteEndArray();
    }

    private static void WriteSourceSchemaErrors(Utf8JsonWriter jsonWriter, ErrorTrie trie)
    {
        if (trie.Error is not null)
        {
            WriteSourceSchemaError(jsonWriter, trie.Error);
        }

        var stack = new Stack<Dictionary<object, ErrorTrie>.ValueCollection.Enumerator>();
        stack.Push(trie.Values.GetEnumerator());

        while (stack.TryPop(out var children))
        {
            if (!children.MoveNext())
            {
                continue;
            }

            var child = children.Current;
            stack.Push(children);

            if (child.Error is not null)
            {
                WriteSourceSchemaError(jsonWriter, child.Error);
            }

            stack.Push(child.Values.GetEnumerator());
        }
    }

    private static void WriteSourceSchemaError(Utf8JsonWriter jsonWriter, IError error)
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("message", error.Message);

        if (error.Path is not null)
        {
            jsonWriter.WritePropertyName("path");
            jsonWriter.WriteStartArray();

            foreach (var segment in error.Path.ToList())
            {
                switch (segment)
                {
                    case int index:
                        jsonWriter.WriteNumberValue(index);
                        break;

                    default:
                        jsonWriter.WriteStringValue(segment.ToString());
                        break;
                }
            }

            jsonWriter.WriteEndArray();
        }

        if (error.Extensions is { Count: > 0 } extensions)
        {
            jsonWriter.WritePropertyName("extensions");
            jsonWriter.WriteStartObject();

            foreach (var (key, value) in extensions)
            {
                jsonWriter.WritePropertyName(key);

                switch (value)
                {
                    case RawJsonValue rawJson:
                        jsonWriter.WriteRawValue(rawJson.Value.Span);
                        break;

                    default:
                        JsonSerializer.Serialize(jsonWriter, value);
                        break;
                }
            }

            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndObject();
    }

    private static void WriteSourceSchemaDocument(YamlWriter writer, string schemaText)
    {
        var document = Utf8GraphQLParser.Parse(schemaText);

        document = document.WithDefinitions(
            document.Definitions.Where(IsDefinitionIncluded).ToArray());

        WriteLiteral(writer, document.ToString(indented: true));
    }

    private static void WriteOperationRequest(
        YamlWriter writer,
        OperationRequest request,
        RawRequest? rawRequest)
    {
        if (request.OnError is not null && request.OnError != ErrorHandlingMode.Propagate)
        {
            writer.WritePropertyName("onError");
            writer.WriteStringValue(request.OnError.ToString());
        }

        if (rawRequest is not null)
        {
            var contentType = rawRequest.ContentType;

            if (contentType.MediaType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) == true)
            {
                WriteMultipartRequest(writer, contentType, rawRequest.Body);
            }
            else
            {
                var streamReader = new StreamReader(rawRequest.Body);
                var rawRequestString = streamReader.ReadToEnd();

                WriteRawRequest(writer, contentType.MediaType, rawRequestString);
            }

            return;
        }

        WriteDocument(writer, request.Query!);

        if (request.Variables is not null)
        {
            writer.WritePropertyName("variables");
            WriteLiteral(
                writer,
                JsonSerializer.Serialize(
                    request.Variables,
                    new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static void WriteDocument(YamlWriter writer, string query)
    {
        writer.WritePropertyName("document");

        // Ensure consistent formatting
        WriteLiteral(writer, Utf8GraphQLParser.Parse(query).ToString(indented: true));
    }

    private static void TryWriteOnError(YamlWriter writer, JsonElement requestElement)
    {
        if (requestElement.ValueKind is not JsonValueKind.Object
            || !requestElement.TryGetProperty("onError", out var onErrorProperty)
            || onErrorProperty.ValueKind is not JsonValueKind.String
            || !Enum.TryParse<ErrorHandlingMode>(
                onErrorProperty.GetString(),
                ignoreCase: true,
                out var onError))
        {
            return;
        }

        writer.WritePropertyName("onError");
        writer.WriteStringValue(onError.ToString());
    }

    private static void WriteFormattedJson(YamlWriter writer, JsonElement json)
    {
        var memoryStream = new MemoryStream();

        using (var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteTo(jsonWriter);
        }

        WriteLiteral(writer, Encoding.UTF8.GetString(memoryStream.ToArray()));
    }

    // Writes the text as a literal block that ends with a single line break, like the
    // line-by-line output the snapshots had before.
    private static void WriteLiteral(YamlWriter writer, string text)
        => writer.WriteLiteralStringValue(text.ReplaceLineEndings("\n").TrimEnd('\n') + "\n");

    private static void WriteMultipartRequest(
        YamlWriter writer,
        MediaTypeHeaderValue contentType,
        MemoryStream body)
    {
        var streamReader = new StreamReader(body);
        var rawRequestString = streamReader.ReadToEnd();
        var contentTypeString = contentType.MediaType;

        var boundary = contentType.Parameters
            .FirstOrDefault(
                p => p.Name.Equals("boundary", StringComparison.OrdinalIgnoreCase))
            ?.Value?.Trim('"');

        // The multipart boundary is random, so we need to change it to a fixed value for the snapshots
        if (!string.IsNullOrEmpty(boundary))
        {
            const string staticBoundaryValue = "f56524ab-5626-4955-b296-234a097b44f6";

            contentTypeString = $"{contentType.MediaType}; boundary=\"{staticBoundaryValue}\"";

            rawRequestString = rawRequestString.Replace($"--{boundary}", $"--{staticBoundaryValue}");
        }

        WriteRawRequest(writer, contentTypeString, rawRequestString);
    }

    private static void WriteRawRequest(YamlWriter writer, string? contentType, string body)
    {
        if (!string.IsNullOrEmpty(contentType))
        {
            writer.WritePropertyName("contentType");
            writer.WriteStringValue(contentType);
        }

        writer.WritePropertyName("body");
        WriteLiteral(writer, body);
    }

    private static readonly FrozenSet<string> s_testDirectives = new HashSet<string> { "null", "error", "returns" }
        .ToFrozenSet();

    private static bool IsDefinitionIncluded(IDefinitionNode node)
    {
        if (node is DirectiveDefinitionNode directive)
        {
            return !FusionBuiltIns.SourceSchemaDirectives.ContainsKey(directive.Name.Value)
                && !s_testDirectives.Contains(directive.Name.Value);
        }

        if (node is ScalarTypeDefinitionNode scalar)
        {
            return !FusionBuiltIns.SourceSchemaScalars.ContainsKey(scalar.Name.Value);
        }

        return true;
    }

    [GeneratedRegex(@" in (\S+\.cs):line \d+", RegexOptions.CultureInvariant)]
    private static partial Regex StackTraceLineRegex();

    protected class RawRequest
    {
        public required MemoryStream Body { get; init; }

        public required MediaTypeHeaderValue ContentType { get; init; }
    }
}
