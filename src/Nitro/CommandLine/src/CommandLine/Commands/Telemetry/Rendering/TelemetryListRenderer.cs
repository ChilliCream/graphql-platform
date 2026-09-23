using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using HotChocolate.Buffers;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;

internal sealed class TelemetryListRenderer(INitroConsole console)
{
    private const string NarrowingHint = "narrow with --since, --service or --filter, or raise --limit";
    private static readonly JsonWriterOptions s_jsonWriterOptions = new() { Indented = false };

    public void Render<TItem>(
        IReadOnlyList<TItem> items,
        int? total,
        bool hasMore,
        JsonTypeInfo<TItem> jsonTypeInfo)
        => Render(items, total, hasMore, jsonTypeInfo, emptyResultHint: null);

    public void Render<TItem>(
        IReadOnlyList<TItem> items,
        int? total,
        bool hasMore,
        JsonTypeInfo<TItem> jsonTypeInfo,
        string? emptyResultHint)
    {
        var hint = items.Count == 0 && emptyResultHint is not null
            ? emptyResultHint
            : CreateHint(items.Count, total, hasMore);

        RenderJsonEnvelope(items, total, hasMore, hint, jsonTypeInfo);
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
}
