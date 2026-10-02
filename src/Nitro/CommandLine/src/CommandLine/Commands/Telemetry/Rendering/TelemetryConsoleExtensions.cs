using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using HotChocolate.Buffers;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;

internal static class TelemetryConsoleExtensions
{
    private static readonly JsonWriterOptions s_jsonWriterOptions = new() { Indented = true };

    public static void WriteListEnvelope<TItem>(
        this INitroConsole console,
        IReadOnlyList<TItem> items,
        int? total,
        bool hasMore,
        JsonTypeInfo<TItem> jsonTypeInfo,
        string? emptyResultHint,
        ReadOnlySpan<Option> narrowingOptions)
    {
        var hint = items.Count == 0 && emptyResultHint is not null
            ? emptyResultHint
            : CreateHint(narrowingOptions, items.Count, total, hasMore);

        using var buffer = new PooledArrayWriter();
        WriteEnvelope(buffer, items, total, hasMore, hint, jsonTypeInfo);

        console.WriteRawLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    private static string? CreateHint(
        ReadOnlySpan<Option> narrowingOptions,
        int returned,
        int? total,
        bool hasMore)
    {
        if (!hasMore)
        {
            return null;
        }

        var shown = total is { } value
            ? $"showing {returned} of {value} (more)"
            : $"showing {returned} (more)";

        return $"{shown}, {CreateNarrowingAdvice(narrowingOptions)}";
    }

    private static string CreateNarrowingAdvice(ReadOnlySpan<Option> narrowingOptions)
    {
        if (narrowingOptions.Length == 0)
        {
            return "raise --limit";
        }

        var names = new string[narrowingOptions.Length];

        for (var i = 0; i < names.Length; i++)
        {
            names[i] = narrowingOptions[i].Name;
        }

        var narrowing = names.Length == 1
            ? names[0]
            : $"{string.Join(", ", names, 0, names.Length - 1)} or {names[^1]}";

        return $"narrow with {narrowing}, or raise --limit";
    }

    private static void WriteEnvelope<TItem>(
        IBufferWriter<byte> buffer,
        IReadOnlyList<TItem> items,
        int? total,
        bool hasMore,
        string? hint,
        JsonTypeInfo<TItem> jsonTypeInfo)
    {
        using var writer = new Utf8JsonWriter(buffer, s_jsonWriterOptions);

        writer.WriteStartObject();
        writer.WriteStartArray("items");

        for (var i = 0; i < items.Count; i++)
        {
            JsonSerializer.Serialize(writer, items[i], jsonTypeInfo);
        }

        writer.WriteEndArray();
        writer.WriteNumber("returned", items.Count);

        if (total is { } totalValue)
        {
            writer.WriteNumber("total", totalValue);
        }
        else
        {
            writer.WriteNull("total");
        }

        writer.WriteBoolean("hasMore", hasMore);

        if (hint is not null)
        {
            writer.WriteString("hint", hint);
        }

        writer.WriteEndObject();
        writer.Flush();
    }
}
