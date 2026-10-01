using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using HotChocolate.Buffers;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;

internal static class TelemetryConsoleExtensions
{
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
            : narrowingOptions.CreateHint(items.Count, total, hasMore);

        using var buffer = new PooledArrayWriter();
        buffer.WriteEnvelope(items, total, hasMore, hint, jsonTypeInfo);

        console.WriteRawLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}

file static class Extensions
{
    private static readonly JsonWriterOptions s_jsonWriterOptions = new() { Indented = true };

    extension(ReadOnlySpan<Option> narrowingOptions)
    {
        public string? CreateHint(int returned, int? total, bool hasMore)
        {
            if (!hasMore)
            {
                return null;
            }

            var shown = total is { } value
                ? $"showing {returned} of {value} (more)"
                : $"showing {returned} (more)";

            return $"{shown}, {narrowingOptions.CreateNarrowingAdvice()}";
        }

        private string CreateNarrowingAdvice()
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
    }

    extension(IBufferWriter<byte> buffer)
    {
        public void WriteEnvelope<TItem>(
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
}
