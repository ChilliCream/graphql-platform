using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ChilliCream.Nitro.CommandLine.Results;
using HotChocolate.Buffers;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;

internal sealed class TelemetryListRenderer(INitroConsole console)
{
    private const int MaximumTableValueLength = 120;
    private const string NarrowingHint = "narrow with --since, --service or --filter, or raise --limit";
    private static readonly JsonWriterOptions s_jsonWriterOptions = new() { Indented = false };

    public void Render<TItem>(
        IReadOnlyList<TItem> items,
        int? total,
        bool hasMore,
        string things,
        JsonTypeInfo<TItem> jsonTypeInfo,
        params TelemetryListColumn<TItem>[] columns)
    {
        var hint = CreateHint(items.Count, total, hasMore);

        if (console.OutputFormat is OutputFormat.Ndjson)
        {
            RenderNdjson(items, jsonTypeInfo);
            return;
        }

        if (console.IsAgentMode || console.OutputFormat is OutputFormat.Json)
        {
            RenderJsonEnvelope(items, total, hasMore, hint, jsonTypeInfo);
            return;
        }

        RenderTable(items, things, columns, hint);
    }

    private static string? CreateHint(int returned, int? total, bool hasMore)
    {
        if (!hasMore)
        {
            return null;
        }

        var shown = total is { } value
            ? $"showing {returned} of {value} (more)"
            : $"showing {returned} (more)";

        return $"{shown}, {NarrowingHint}";
    }

    private void RenderNdjson<TItem>(
        IReadOnlyList<TItem> items,
        JsonTypeInfo<TItem> jsonTypeInfo)
    {
        foreach (var item in items)
        {
            console.WriteRawLine(Serialize(item, jsonTypeInfo));
        }
    }

    private void RenderJsonEnvelope<TItem>(
        IReadOnlyList<TItem> items,
        int? total,
        bool hasMore,
        string? hint,
        JsonTypeInfo<TItem> jsonTypeInfo)
    {
        var output = new StringBuilder();

        if (items.Count == 0)
        {
            output.Append("{\"items\":[]");
        }
        else
        {
            output.AppendLine("{\"items\":[");

            for (var i = 0; i < items.Count; i++)
            {
                output.Append(Serialize(items[i], jsonTypeInfo));

                if (i < items.Count - 1)
                {
                    output.AppendLine(",");
                }
                else
                {
                    output.AppendLine();
                }
            }

            output.Append(']');
        }

        output.Append(",\"returned\":");
        output.Append(items.Count);
        output.Append(",\"total\":");

        if (total is { } totalValue)
        {
            output.Append(totalValue);
        }
        else
        {
            output.Append("null");
        }

        output.Append(",\"hasMore\":");
        output.Append(hasMore ? "true" : "false");

        if (hint is not null)
        {
            output.Append(",\"hint\":");
            output.Append('"');
            output.Append(JsonEncodedText.Encode(hint));
            output.Append('"');
        }

        output.Append('}');
        console.WriteRawLine(output.ToString());
    }

    private static string Serialize<TItem>(TItem item, JsonTypeInfo<TItem> jsonTypeInfo)
    {
        using var buffer = new PooledArrayWriter();

        using (var writer = new Utf8JsonWriter(buffer, s_jsonWriterOptions))
        {
            JsonSerializer.Serialize(writer, item, jsonTypeInfo);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private void RenderTable<TItem>(
        IReadOnlyList<TItem> items,
        string things,
        IReadOnlyList<TelemetryListColumn<TItem>> columns,
        string? hint)
    {
        if (items.Count == 0)
        {
            console.WriteLine($"No {things} found.");
            return;
        }

        var table = new Table();

        foreach (var column in columns)
        {
            table.AddColumn(column.Header);
        }

        foreach (var item in items)
        {
            table.AddRow(
                columns
                    .Select(column => (IRenderable)new Text(Truncate(column.Value(item))))
                    .ToArray());
        }

        console.Write(table);

        if (hint is not null)
        {
            console.WriteLine(hint);
        }
    }

    internal static string Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaximumTableValueLength)
        {
            return value ?? string.Empty;
        }

        return string.Concat(value.AsSpan(0, MaximumTableValueLength - 1), "…");
    }
}
