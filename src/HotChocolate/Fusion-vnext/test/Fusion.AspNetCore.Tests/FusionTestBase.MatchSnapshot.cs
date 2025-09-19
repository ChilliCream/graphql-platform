using System.Collections.Frozen;
using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Language;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;
using OperationResult = HotChocolate.Transport.OperationResult;

namespace HotChocolate.Fusion;

public abstract partial class FusionTestBase
{
    protected async Task MatchSnapshotAsync(
        Gateway gateway,
        OperationRequest request,
        GraphQLHttpResponse response,
        string? postFix = null)
    {
        var snapshot = new Snapshot(postFix, ".yaml");

        var results = new List<OperationResult>();

        // We first wait and capture all possible gateway responses.
        await foreach (var result in response.ReadAsResultStreamAsync())
        {
            results.Add(result);
        }

        var testServerRegistrations = gateway.Services
            .GetServices<TestServerRegistration>()
            .ToArray();

        var sb = new StringBuilder();
        var writer = new CodeWriter(sb);

        writer.WriteLine("title: {0}", snapshot.Title);

        writer.WriteLine("request:");
        writer.Indent();
        WriteOperationRequest(writer, request);
        writer.Unindent();

        WriteResults(writer, results);

        writer.WriteLine("sourceSchemas:");
        writer.Indent();

        foreach (var sourceSchema in gateway.SourceSchemas)
        {
            WriteSourceSchema(writer, gateway, testServerRegistrations, sourceSchema);
        }

        writer.Unindent();

        await TryWriteOperationPlanAsync(writer, gateway, results);

        snapshot.Add(sb.ToString());

        foreach (var result in results)
        {
            result.Dispose();
        }

        gateway.Interactions.Clear();

        await snapshot.MatchAsync();
    }

    private static async Task TryWriteOperationPlanAsync(
        CodeWriter writer,
        Gateway gateway,
        List<OperationResult> results)
    {
        foreach (var result in results)
        {
            try
            {
                if (result.Extensions.TryGetProperty("fusion", out var fusionProperty)
                    && fusionProperty.TryGetProperty("operationPlan", out var operationPlanProperty))
                {
                    var manager = gateway.Services.GetRequiredService<FusionRequestExecutorManager>();
                    var executor = await manager.GetExecutorAsync();
                    var operationCompiler = executor.Schema.Services.GetRequiredService<OperationCompiler>();
                    var parser = new JsonOperationPlanParser(operationCompiler);

                    var buffer = new PooledArrayWriter();
                    await using var jsonWriter = new Utf8JsonWriter(buffer);

                    operationPlanProperty.WriteTo(jsonWriter);
                    await jsonWriter.FlushAsync();

                    var plan = parser.Parse(buffer.WrittenMemory);

                    var operationPlanFormatter = new YamlOperationPlanFormatter();
                    var formattedOperationPlan = operationPlanFormatter.Format(plan);

                    writer.WriteLine("operationPlan:");
                    writer.Indent();
                    WriteMultilineString(writer, formattedOperationPlan);
                    writer.Unindent();

                    break;
                }
            }
            catch
            {
                // For some reason
                // CancellationTests.Request_Is_Running_Into_Execution_Timeout_While_Http_Request_In_Node_Is_Still_Ongoing
                // runs into an issue here
            }
        }
    }

    private void WriteResults(CodeWriter writer, List<OperationResult> results)
    {
        if (results is [{ } singleResult])
        {
            writer.WriteLine("response:");
            writer.Indent();
            writer.WriteLine("body: |");
            writer.Indent();

            WriteResult(writer, singleResult);

            writer.Unindent();
            writer.Unindent();
        }
        else
        {
            writer.WriteLine("responseStream:");
            writer.Indent();

            foreach (var result in results)
            {
                writer.WriteLine("- body: |");
                writer.Indent();
                writer.Indent();
                WriteResult(writer, result);
                writer.Unindent();
                writer.Unindent();
            }

            writer.Unindent();
        }
    }

    private static void WriteResult(CodeWriter writer, OperationResult result)
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

        memoryStream.Position = 0;

        var reader = new StreamReader(memoryStream);
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
    }

    private static void WriteSourceSchema(
        CodeWriter writer,
        Gateway gateway,
        TestServerRegistration[] testServerRegistrations,
        SourceSchemaText sourceSchemaText)
    {
        var sourceSchemaName = sourceSchemaText.Name;
        var testServer = testServerRegistrations.First(r => r.Name == sourceSchemaName);

        writer.WriteLine("- name: {0}", sourceSchemaName);
        writer.Indent();
        writer.WriteLine("schema: |");
        writer.Indent();
        WriteSourceSchemaDocument(writer, sourceSchemaText.SourceText);
        writer.Unindent();

        if (testServer.Options.IsTimingOut)
        {
            writer.WriteLine("isTimingOut: true");
        }

        var interactions = gateway.Interactions.GetValueOrDefault(sourceSchemaName);

        if (interactions is not null)
        {
            writer.WriteLine("interactions:");
            writer.Indent();

            foreach (var (_, interaction) in interactions.OrderBy(x => x.Key))
            {
                writer.WriteLine("- request:");
                writer.Indent();
                writer.Indent();
                writer.WriteLine("document: |");
                writer.Indent();

                var query = interaction.Request!.Query.GetString()!;
                // Ensure consistent formatting
                var document = Utf8GraphQLParser.Parse(query).ToString(indented: true);

                WriteMultilineString(writer, document);
                writer.Unindent();

                if (interaction.Request!.Variables.ValueKind != JsonValueKind.Undefined)
                {
                    writer.WriteLine("variables: |");
                    writer.Indent();
                    WriteFormattedJson(writer, interaction.Request.Variables);
                    writer.Unindent();
                }

                writer.Unindent();

                if (interaction.StatusCode.HasValue)
                {
                    writer.WriteLine("response:");
                    writer.Indent();

                    if (interaction.StatusCode.HasValue && interaction.StatusCode != HttpStatusCode.OK)
                    {
                        writer.WriteLine("statusCode: {0}", (int)interaction.StatusCode);
                    }

                    if (interaction.Results.Count > 0)
                    {
                        writer.WriteLine("results:");
                        writer.Indent();

                        foreach (var result in interaction.Results)
                        {
                            writer.WriteLine("- |");
                            writer.Indent();
                            WriteMultilineString(writer, result);
                            writer.Unindent();
                        }

                        writer.Unindent();
                    }

                    writer.Unindent();
                }

                writer.Unindent();
            }

            writer.Unindent();
        }

        writer.Unindent();
    }

    private static string SerializeSourceSchemaResult(SourceSchemaResult result)
    {
        var memoryStream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

        jsonWriter.WriteStartObject();

        if (result.RawErrors.ValueKind != JsonValueKind.Undefined)
        {
            jsonWriter.WritePropertyName("errors");
            result.RawErrors.WriteTo(jsonWriter);
        }

        if (result.Data.ValueKind != JsonValueKind.Undefined)
        {
            jsonWriter.WritePropertyName("data");
            result.Data.WriteTo(jsonWriter);
        }

        if (result.Extensions.ValueKind != JsonValueKind.Undefined)
        {
            jsonWriter.WritePropertyName("extensions");
            result.Extensions.WriteTo(jsonWriter);
        }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        memoryStream.Position = 0;

        var reader = new StreamReader(memoryStream);
        return reader.ReadToEnd();
    }

    private static void WriteSourceSchemaDocument(CodeWriter writer, string schemaText)
    {
        var document = Utf8GraphQLParser.Parse(schemaText);

        document = document.WithDefinitions(
            document.Definitions.Where(IsDefinitionIncluded).ToArray());

        var cleanedSchema = document.ToString(indented: true);

        WriteMultilineString(writer, cleanedSchema);
    }

    private static void WriteOperationRequest(CodeWriter writer, OperationRequest request)
    {
        if (request.OnError is not null && request.OnError != ErrorHandlingMode.Propagate)
        {
            writer.WriteLine("onError: {0}", request.OnError);
        }

        writer.WriteLine("document: |");
        writer.Indent();

        // Ensure consistent formatting
        var document = Utf8GraphQLParser.Parse(request.Query!).ToString(indented: true);

        WriteMultilineString(writer, document);
        writer.Unindent();

        if (request.Variables is not null)
        {
            writer.WriteLine("variables: |");
            writer.Indent();

            var jsonVariables = JsonSerializer.Serialize(
                request.Variables,
                new JsonSerializerOptions { WriteIndented = true });

            WriteMultilineString(writer, jsonVariables);

            writer.Unindent();
        }
    }

    private static void WriteFormattedJson(CodeWriter writer, JsonElement json)
    {
        var memoryStream = new MemoryStream();
        var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

        json.WriteTo(jsonWriter);
        jsonWriter.Flush();

        memoryStream.Position = 0;

        var reader = new StreamReader(memoryStream);
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
    }

    private static void WriteMultilineString(CodeWriter writer, string multilineString)
    {
        var reader = new StringReader(multilineString);
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
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
}
