using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CookieCrumble.HotChocolate.Formatters;

/// <summary>
/// Shared helpers for stable (deterministic) snapshot formatters.
/// Provides canonical JSON writing, stream accumulation, and sort-key logic
/// used by both <see cref="StableExecutionResultSnapshotValueFormatter"/>
/// and <see cref="StableGraphQLHttpResponseFormatter"/>.
/// </summary>
internal static class StableSnapshotHelpers
{
    public static readonly JsonWriterOptions IndentedWriterOptions = new()
    {
        Indented = true,
        SkipValidation = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static readonly JsonWriterOptions CompactWriterOptions = new()
    {
        Indented = false,
        SkipValidation = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    internal static bool TryReadId(JsonElement element, out string id)
    {
        if (!element.TryGetProperty("id", out var idElement))
        {
            id = string.Empty;
            return false;
        }

        switch (idElement.ValueKind)
        {
            case JsonValueKind.String:
                id = idElement.GetString() ?? string.Empty;
                return !string.IsNullOrEmpty(id);

            case JsonValueKind.Number:
                if (idElement.TryGetInt64(out var int64Id))
                {
                    id = int64Id.ToString(CultureInfo.InvariantCulture);
                    return true;
                }

                id = idElement.GetRawText();
                return !string.IsNullOrEmpty(id);

            default:
                id = idElement.GetRawText();
                return !string.IsNullOrEmpty(id);
        }
    }

    public static void WriteStableStreamSnapshot(
        Utf8JsonWriter writer,
        StreamAccumulator acc,
        JsonElement mergedResult)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", "stable-stream");
        writer.WriteNumber("payloadCount", acc.PayloadCount);

        if (acc.InitialPayload is { } initial)
        {
            writer.WritePropertyName("initial");
            WriteCanonicalResponseObject(writer, initial);
        }

        WritePending(writer, acc.PendingById);
        WriteIncremental(writer, acc.IncrementalEntries, acc.PendingById);
        WriteCompleted(writer, acc.CompletedEntries);
        WriteRootErrors(writer, acc.RootErrors);

        WriteDiagnostics(writer, acc);

        writer.WritePropertyName("final");
        WriteCanonicalResponseObject(writer, mergedResult);

        writer.WriteEndObject();
    }

    public static void WritePending(
        Utf8JsonWriter writer,
        Dictionary<string, PendingEntry> pendingById)
    {
        if (pendingById.Count == 0)
        {
            return;
        }

        var pending = pendingById.Values.ToList();
        pending.Sort(static (x, y) => CompareIds(x.Id, y.Id));

        writer.WritePropertyName("pending");
        writer.WriteStartArray();

        foreach (var entry in pending)
        {
            writer.WriteStartObject();
            writer.WriteString("id", entry.Id);
            writer.WritePropertyName("path");
            WriteCanonicalJson(writer, entry.Path);

            if (!string.IsNullOrEmpty(entry.Label))
            {
                writer.WriteString("label", entry.Label);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static void WriteIncremental(
        Utf8JsonWriter writer,
        List<IncrementalEntry> incrementalEntries,
        Dictionary<string, PendingEntry> pendingById)
    {
        if (incrementalEntries.Count == 0)
        {
            return;
        }

        var incremental = incrementalEntries.ToList();
        incremental.Sort(
            (x, y) =>
            {
                var c = CompareIds(x.Id, y.Id);
                if (c != 0)
                {
                    return c;
                }

                c = string.CompareOrdinal(GetPathSortKey(x, pendingById), GetPathSortKey(y, pendingById));
                if (c != 0)
                {
                    return c;
                }

                c = string.CompareOrdinal(GetSubPathSortKey(x), GetSubPathSortKey(y));
                if (c != 0)
                {
                    return c;
                }

                c = string.CompareOrdinal(GetPayloadKindSortKey(x), GetPayloadKindSortKey(y));
                if (c != 0)
                {
                    return c;
                }

                c = string.CompareOrdinal(GetPayloadValueSortKey(x), GetPayloadValueSortKey(y));
                if (c != 0)
                {
                    return c;
                }

                return string.CompareOrdinal(GetErrorsSortKey(x.Errors), GetErrorsSortKey(y.Errors));
            });

        writer.WritePropertyName("incremental");
        writer.WriteStartArray();

        foreach (var entry in incremental)
        {
            writer.WriteStartObject();
            writer.WriteString("id", entry.Id);

            if (pendingById.TryGetValue(entry.Id, out var pending))
            {
                writer.WritePropertyName("path");
                WriteCanonicalJson(writer, pending.Path);
            }

            if (entry.SubPath is { } subPath)
            {
                writer.WritePropertyName("subPath");
                WriteCanonicalJson(writer, subPath);
            }

            if (entry.Data is { } data)
            {
                writer.WritePropertyName("data");
                WriteCanonicalJson(writer, data);
            }

            if (entry.Items is { } items)
            {
                writer.WritePropertyName("items");
                WriteCanonicalJson(writer, items);
            }

            if (entry.Errors is { } errors)
            {
                writer.WritePropertyName("errors");
                WriteCanonicalJson(writer, errors);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static void WriteCompleted(
        Utf8JsonWriter writer,
        List<CompletedEntry> completedEntries)
    {
        if (completedEntries.Count == 0)
        {
            return;
        }

        var completed = completedEntries.ToList();
        completed.Sort(
            (x, y) =>
            {
                var c = CompareIds(x.Id, y.Id);
                if (c != 0)
                {
                    return c;
                }

                return string.CompareOrdinal(GetErrorsSortKey(x.Errors), GetErrorsSortKey(y.Errors));
            });

        writer.WritePropertyName("completed");
        writer.WriteStartArray();

        foreach (var entry in completed)
        {
            writer.WriteStartObject();
            writer.WriteString("id", entry.Id);

            if (entry.Errors is { } errors)
            {
                writer.WritePropertyName("errors");
                WriteCanonicalJson(writer, errors);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static void WriteDiagnostics(
        Utf8JsonWriter writer,
        StreamAccumulator acc)
    {
        var pendingIds = new HashSet<string>(acc.PendingById.Keys);
        var completedIds = new HashSet<string>(acc.CompletedEntries.Select(c => c.Id));

        var neverCompleted = pendingIds.Except(completedIds).Order().ToList();
        var completedWithoutPending = completedIds.Except(pendingIds).Order().ToList();

        if (neverCompleted.Count == 0 && completedWithoutPending.Count == 0)
        {
            return;
        }

        writer.WritePropertyName("diagnostics");
        writer.WriteStartObject();

        if (neverCompleted.Count > 0)
        {
            writer.WritePropertyName("pendingNeverCompleted");
            writer.WriteStartArray();
            foreach (var id in neverCompleted)
            {
                writer.WriteStringValue(id);
            }
            writer.WriteEndArray();
        }

        if (completedWithoutPending.Count > 0)
        {
            writer.WritePropertyName("completedWithoutPending");
            writer.WriteStartArray();
            foreach (var id in completedWithoutPending)
            {
                writer.WriteStringValue(id);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    public static void WriteRootErrors(
        Utf8JsonWriter writer,
        List<JsonElement> rootErrors)
    {
        if (rootErrors.Count == 0)
        {
            return;
        }

        var errors = rootErrors.ToList();
        errors.Sort(
            static (x, y) =>
                string.CompareOrdinal(
                    BuildCanonicalJsonSortKey(x),
                    BuildCanonicalJsonSortKey(y)));

        writer.WritePropertyName("rootErrors");
        writer.WriteStartArray();

        foreach (var error in errors)
        {
            WriteCanonicalJson(writer, error);
        }

        writer.WriteEndArray();
    }

    public static void WriteCanonicalJson(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = element.EnumerateObject().ToList();
                properties.Sort(static (x, y) => string.CompareOrdinal(x.Name, y.Name));

                writer.WriteStartObject();

                foreach (var property in properties)
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalJson(writer, property.Value);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();

                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonicalJson(writer, item);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                element.WriteTo(writer);
                break;

            default:
                writer.WriteNullValue();
                break;
        }
    }

    /// <summary>
    /// Writes a response-shape object (initial or final merged result) with a
    /// canonical property order. Strips incremental-delivery protocol fields
    /// (captured separately) and the <c>fusion</c> extension (captured
    /// separately by the test harness as the rendered operation plan). The
    /// <c>extensions</c> object is omitted entirely when it contains nothing
    /// beyond <c>fusion</c>; otherwise the non-fusion extensions are kept.
    /// </summary>
    public static void WriteCanonicalResponseObject(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        if (element.ValueKind is not JsonValueKind.Object)
        {
            WriteCanonicalJson(writer, element);
            return;
        }

        var properties = element.EnumerateObject().ToList();
        properties.Sort(static (x, y) => string.CompareOrdinal(x.Name, y.Name));

        writer.WriteStartObject();

        foreach (var property in properties)
        {
            if (IsStreamField(property.Name))
            {
                continue;
            }

            if (string.Equals(property.Name, "extensions", StringComparison.Ordinal))
            {
                WriteExtensionsWithoutFusion(writer, property.Value);
                continue;
            }

            writer.WritePropertyName(property.Name);
            WriteCanonicalJson(writer, property.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteExtensionsWithoutFusion(
        Utf8JsonWriter writer,
        JsonElement extensions)
    {
        if (extensions.ValueKind is not JsonValueKind.Object)
        {
            writer.WritePropertyName("extensions");
            WriteCanonicalJson(writer, extensions);
            return;
        }

        var remaining = new List<JsonProperty>();

        foreach (var extension in extensions.EnumerateObject())
        {
            if (string.Equals(extension.Name, "fusion", StringComparison.Ordinal))
            {
                continue;
            }

            remaining.Add(extension);
        }

        if (remaining.Count == 0)
        {
            return;
        }

        remaining.Sort(static (x, y) => string.CompareOrdinal(x.Name, y.Name));

        writer.WritePropertyName("extensions");
        writer.WriteStartObject();

        foreach (var extension in remaining)
        {
            writer.WritePropertyName(extension.Name);
            WriteCanonicalJson(writer, extension.Value);
        }

        writer.WriteEndObject();
    }

    public static string BuildCanonicalJsonSortKey(JsonElement element)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, CompactWriterOptions);
        WriteCanonicalJson(writer, element);
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static int CompareIds(string x, string y)
    {
        var xIsNumeric = int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var xId);
        var yIsNumeric = int.TryParse(y, NumberStyles.Integer, CultureInfo.InvariantCulture, out var yId);

        if (xIsNumeric && yIsNumeric)
        {
            return xId.CompareTo(yId);
        }

        if (xIsNumeric)
        {
            return -1;
        }

        if (yIsNumeric)
        {
            return 1;
        }

        return string.CompareOrdinal(x, y);
    }

    public static bool IsStreamField(string fieldName)
        => fieldName is "hasNext" or "pending" or "incremental" or "completed";

    private static string GetPathSortKey(
        IncrementalEntry entry,
        Dictionary<string, PendingEntry> pendingById)
        => pendingById.TryGetValue(entry.Id, out var pending)
            ? BuildCanonicalJsonSortKey(pending.Path)
            : string.Empty;

    private static string GetSubPathSortKey(IncrementalEntry entry)
        => entry.SubPath is { } subPath
            ? BuildCanonicalJsonSortKey(subPath)
            : string.Empty;

    private static string GetPayloadKindSortKey(IncrementalEntry entry)
    {
        if (entry.Data is not null)
        {
            return "data";
        }

        if (entry.Items is not null)
        {
            return "items";
        }

        return string.Empty;
    }

    private static string GetPayloadValueSortKey(IncrementalEntry entry)
    {
        if (entry.Data is { } data)
        {
            return BuildCanonicalJsonSortKey(data);
        }

        if (entry.Items is { } items)
        {
            return BuildCanonicalJsonSortKey(items);
        }

        return string.Empty;
    }

    private static string GetErrorsSortKey(JsonElement? errors)
        => errors is { } e
            ? BuildCanonicalJsonSortKey(e)
            : string.Empty;

    internal sealed class StreamAccumulator
    {
        public int PayloadCount { get; private set; }

        public JsonElement? InitialPayload { get; private set; }

        public Dictionary<string, PendingEntry> PendingById { get; } = [];

        public List<IncrementalEntry> IncrementalEntries { get; } = [];

        public List<CompletedEntry> CompletedEntries { get; } = [];

        public List<JsonElement> RootErrors { get; } = [];

        public void AddPayload(JsonElement root)
        {
            PayloadCount++;

            if (InitialPayload is null)
            {
                InitialPayload = root.Clone();
            }

            if (root.TryGetProperty("pending", out var pending))
            {
                foreach (var entry in pending.EnumerateArray())
                {
                    if (!TryReadId(entry, out var id))
                    {
                        continue;
                    }

                    var label = entry.TryGetProperty("label", out var labelElement)
                        && labelElement.ValueKind is JsonValueKind.String
                        ? labelElement.GetString()
                        : null;

                    if (!entry.TryGetProperty("path", out var path))
                    {
                        continue;
                    }

                    PendingById[id] = new PendingEntry(id, path.Clone(), label);
                }
            }

            if (root.TryGetProperty("incremental", out var incremental))
            {
                foreach (var entry in incremental.EnumerateArray())
                {
                    if (!TryReadId(entry, out var id))
                    {
                        continue;
                    }

                    JsonElement? data = null;
                    JsonElement? items = null;
                    JsonElement? subPath = null;
                    JsonElement? errors = null;

                    if (entry.TryGetProperty("data", out var dataElement))
                    {
                        data = dataElement.Clone();
                    }

                    if (entry.TryGetProperty("items", out var itemsElement))
                    {
                        items = itemsElement.Clone();
                    }

                    if (entry.TryGetProperty("subPath", out var subPathElement))
                    {
                        subPath = subPathElement.Clone();
                    }

                    if (entry.TryGetProperty("errors", out var errorsElement))
                    {
                        errors = errorsElement.Clone();
                    }

                    IncrementalEntries.Add(new IncrementalEntry(id, subPath, data, items, errors));
                }
            }

            if (root.TryGetProperty("completed", out var completed))
            {
                foreach (var entry in completed.EnumerateArray())
                {
                    if (!TryReadId(entry, out var id))
                    {
                        continue;
                    }

                    var errors = entry.TryGetProperty("errors", out var errorsElement)
                        ? errorsElement.Clone()
                        : (JsonElement?)null;

                    CompletedEntries.Add(new CompletedEntry(id, errors));
                }
            }

            if (root.TryGetProperty("errors", out var rootErrors)
                && rootErrors.ValueKind is JsonValueKind.Array)
            {
                foreach (var error in rootErrors.EnumerateArray())
                {
                    RootErrors.Add(error.Clone());
                }
            }
        }
    }

    internal sealed record PendingEntry(string Id, JsonElement Path, string? Label);

    internal sealed record IncrementalEntry(
        string Id,
        JsonElement? SubPath,
        JsonElement? Data,
        JsonElement? Items,
        JsonElement? Errors);

    internal sealed record CompletedEntry(string Id, JsonElement? Errors);
}
