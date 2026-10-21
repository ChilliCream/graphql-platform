using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Testing;

public abstract partial class FusionTestBase
{
    /// <summary>
    /// Matches an operation result, subgraph HTTP exchanges, and the execution plan against a YAML snapshot.
    /// </summary>
    public static async Task MatchExchangeSnapshotAsync(
        Snapshot snapshot,
        string document,
        IExecutionResult result,
        IReadOnlyList<HttpExchange> exchanges,
        bool includeRequestOrder)
    {
        var text = new StringBuilder();
        var writer = new CodeWriter(text);
        writer.WriteLine("title: {0}", snapshot.Title);
        writer.WriteLine("request:");
        writer.Indent();
        WriteBlock("document", Utf8GraphQLParser.Parse(document).ToString());
        writer.Unindent();
        writer.WriteLine("response:");
        writer.Indent();
        WriteBlock("body", FormatBody(result.ToJson()));
        writer.Unindent();
        if (includeRequestOrder)
        {
            writer.WriteLine("requestOrder:");
            writer.Indent();

            foreach (var exchange in exchanges)
            {
                writer.WriteLine("- {0}", exchange.SchemaName);
            }

            writer.Unindent();
        }

        writer.WriteLine("sourceSchemas:");
        writer.Indent();

        foreach (var group in exchanges.GroupBy(e => e.SchemaName).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            writer.WriteLine("- name: {0}", group.Key);
            writer.Indent();
            writer.WriteLine("interactions:");
            writer.Indent();

            foreach (var exchange in group)
            {
                writer.WriteLine("- request:");
                writer.Indent();
                writer.Indent();
                WriteBlock("body", FormatBody(exchange.RequestBody));
                writer.Unindent();
                writer.WriteLine("response:");
                writer.Indent();
                writer.WriteLine("statusCode: {0}", exchange.StatusCode?.ToString() ?? "null");
                writer.WriteLine("contentType: {0}", exchange.ContentType ?? "null");
                WriteBlock("body", FormatBody(exchange.ResponseBody));
                writer.Unindent();
                writer.Unindent();
            }

            writer.Unindent();
            writer.Unindent();
        }

        writer.Unindent();
        var plan = Assert.IsType<OperationPlan>(
            result.ExpectOperationResult().Features.Get<IOperationPlan>());
        WriteBlock("operationPlan", new YamlOperationPlanFormatter().Format(plan));
        var snapshotText = StackTraceLineRegex().Replace(text.ToString(), " in $1");
        snapshot.Add(ExchangeSourcePathRegex().Replace(snapshotText, " in $1"));
        await snapshot.MatchAsync();

        void WriteBlock(string name, string value)
        {
            writer.WriteLine("{0}: |", name);
            writer.Indent();
            WriteMultilineString(writer, value.TrimEnd());
            writer.Unindent();
        }
    }

    private static string FormatBody(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch (JsonException)
        {
            return value;
        }
    }

    [GeneratedRegex(@" in \S+?[/\\](src[/\\]\S+?\.cs)", RegexOptions.CultureInvariant)]
    private static partial Regex ExchangeSourcePathRegex();

    /// <summary>
    /// Describes a subgraph HTTP request and its response.
    /// </summary>
    public sealed record HttpExchange(
        string SchemaName,
        string RequestBody,
        int? StatusCode,
        string? ContentType,
        string ResponseBody);
}
