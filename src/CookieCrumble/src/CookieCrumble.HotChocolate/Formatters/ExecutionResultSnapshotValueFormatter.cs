using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;
using CookieCrumble.Formatters;
using HotChocolate;
using HotChocolate.Execution;
using static CookieCrumble.HotChocolate.Formatters.StableSnapshotHelpers;

namespace CookieCrumble.HotChocolate.Formatters;

internal sealed class ExecutionResultSnapshotValueFormatter
    : SnapshotValueFormatter<IExecutionResult>
{
    protected override void Format(IBufferWriter<byte> snapshot, IExecutionResult value)
    {
        if (value.Kind is ExecutionResultKind.SingleResult)
        {
            snapshot.Append(value.ToJson());
        }
        else
        {
            FormatStreamAsync(snapshot, (IResponseStream)value).Wait();
        }
    }

    protected override void FormatMarkdown(IBufferWriter<byte> snapshot, IExecutionResult value)
    {
        if (value.Kind is ExecutionResultKind.SingleResult)
        {
            snapshot.Append("```json");
            snapshot.AppendLine();
            snapshot.Append(value.ToJson());
        }
        else
        {
            snapshot.Append("```text");
            snapshot.AppendLine();
            FormatStreamAsync(snapshot, (IResponseStream)value).Wait();
        }

        snapshot.AppendLine();
        snapshot.Append("```");
        snapshot.AppendLine();
    }

    private static Task FormatStreamAsync(IBufferWriter<byte> snapshot, IResponseStream stream)
        // Only @defer/@stream responses carry the incremental-delivery envelope. Other
        // streams (subscriptions, batches) are sequences of independent results and are
        // written out verbatim, one payload after another.
        => stream.Kind is ExecutionResultKind.DeferredResult
            ? FormatIncrementalAsync(snapshot, stream)
            : FormatEventStreamAsync(snapshot, stream);

    private static async Task FormatEventStreamAsync(IBufferWriter<byte> snapshot, IResponseStream stream)
    {
        await foreach (var result in stream.ReadResultsAsync().ConfigureAwait(false))
        {
            snapshot.Append(result.ToJson());
            snapshot.AppendLine();
        }
    }

    // Reconstructs a single, delivery-order-independent view of an incrementally
    // delivered (@defer/@stream) response: the initial payload's non-protocol
    // fields plus the `pending`, `incremental`, and `completed` entries collected
    // across every payload and ordered by `id`. The snapshot is identical whether
    // the transport bundled the response into one payload or split it across several.
    private static async Task FormatIncrementalAsync(
        IBufferWriter<byte> snapshot,
        IResponseStream stream)
    {
        var accumulator = new StreamAccumulator();

        // StreamAccumulator deep-clones every element it retains, so each parsed
        // document is only needed for the duration of its AddPayload call and can be
        // disposed immediately afterwards.
        await foreach (var result in stream.ReadResultsAsync().ConfigureAwait(false))
        {
            using var document = JsonDocument.Parse(result.ToJson());
            accumulator.AddPayload(document.RootElement);
        }

        await using var writer = new Utf8JsonWriter(snapshot, IndentedWriterOptions);
        WriteEnvelope(writer, accumulator);
        writer.Flush();
        snapshot.AppendLine();
    }

    private static void WriteEnvelope(Utf8JsonWriter writer, StreamAccumulator accumulator)
    {
        writer.WriteStartObject();

        // The initial payload's non-protocol fields (`data`, `errors`, `extensions`) in
        // their original order; the incremental-delivery fields are rebuilt below.
        if (accumulator.InitialPayload is { ValueKind: JsonValueKind.Object } initial)
        {
            foreach (var property in initial.EnumerateObject())
            {
                if (IsStreamField(property.Name))
                {
                    continue;
                }

                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }
        }

        WritePending(writer, accumulator.PendingById);
        WriteIncremental(writer, accumulator.IncrementalEntries);
        WriteCompleted(writer, accumulator.CompletedEntries);

        writer.WriteBoolean("hasNext", false);

        writer.WriteEndObject();
    }

    private static void WritePending(
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
            entry.Path.WriteTo(writer);

            if (!string.IsNullOrEmpty(entry.Label))
            {
                writer.WriteString("label", entry.Label);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteIncremental(
        Utf8JsonWriter writer,
        List<IncrementalEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        // Group by `id` so a stream delivered across several payloads collapses into a
        // single entry, independent of how many frames the transport used.
        var order = new List<string>();
        var byId = new Dictionary<string, List<IncrementalEntry>>();

        foreach (var entry in entries)
        {
            if (!byId.TryGetValue(entry.Id, out var group))
            {
                group = [];
                byId.Add(entry.Id, group);
                order.Add(entry.Id);
            }

            group.Add(entry);
        }

        order.Sort(CompareIds);

        writer.WritePropertyName("incremental");
        writer.WriteStartArray();

        foreach (var id in order)
        {
            var group = byId[id];

            writer.WriteStartObject();
            writer.WriteString("id", id);

            if (group.FirstOrDefault(e => e.SubPath is not null)?.SubPath is { } subPath)
            {
                writer.WritePropertyName("subPath");
                subPath.WriteTo(writer);
            }

            if (group.Any(e => e.Items is not null))
            {
                writer.WritePropertyName("items");
                writer.WriteStartArray();
                foreach (var entry in group)
                {
                    if (entry.Items is { } items)
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            item.WriteTo(writer);
                        }
                    }
                }
                writer.WriteEndArray();
            }
            else if (group.FirstOrDefault(e => e.Data is not null)?.Data is { } data)
            {
                writer.WritePropertyName("data");
                data.WriteTo(writer);
            }

            WriteMergedErrors(writer, group);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteCompleted(
        Utf8JsonWriter writer,
        List<CompletedEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var completed = entries.ToList();
        completed.Sort(static (x, y) => CompareIds(x.Id, y.Id));

        writer.WritePropertyName("completed");
        writer.WriteStartArray();

        foreach (var entry in completed)
        {
            writer.WriteStartObject();
            writer.WriteString("id", entry.Id);

            if (entry.Errors is { } errors)
            {
                writer.WritePropertyName("errors");
                errors.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteMergedErrors(Utf8JsonWriter writer, List<IncrementalEntry> group)
    {
        if (!group.Any(e => e.Errors is not null))
        {
            return;
        }

        writer.WritePropertyName("errors");
        writer.WriteStartArray();

        foreach (var entry in group)
        {
            if (entry.Errors is { } errors)
            {
                foreach (var error in errors.EnumerateArray())
                {
                    error.WriteTo(writer);
                }
            }
        }

        writer.WriteEndArray();
    }
}

internal sealed class JsonResultPatcher
{
    private const string DataProp = "data";
    private const string ItemsProp = "items";
    private const string IncrementalProp = "incremental";
    private const string PendingProp = "pending";
    private const string PathProp = "path";
    private const string SubPathProp = "subPath";
    private const string IdProp = "id";
    private JsonObject? _json;
    private readonly Dictionary<string, JsonElement> _pendingPaths = [];

    public void SetResponse(JsonDocument response)
    {
        ArgumentNullException.ThrowIfNull(response);

        _json = JsonObject.Create(response.RootElement);
        ProcessPayload(response.RootElement);
    }

    public void ApplyPatch(JsonDocument patch)
    {
        if (_json is null)
        {
            throw new InvalidOperationException(
                "You must first set the initial response before you can apply patches.");
        }

        ProcessPayload(patch.RootElement);
    }

    public void WriteResponse(IBufferWriter<byte> snapshot)
    {
        if (_json is null)
        {
            throw new InvalidOperationException(
                "You must first set the initial response before you can apply patches.");
        }

        using var writer = new Utf8JsonWriter(snapshot, new JsonWriterOptions { Indented = true });

        _json.Remove("hasNext");
        _json.Remove("pending");
        _json.Remove("incremental");
        _json.Remove("completed");

        _json.WriteTo(writer);
        writer.Flush();
    }

    private void ProcessPayload(JsonElement root)
    {
        if (root.TryGetProperty(PendingProp, out var pending))
        {
            foreach (var entry in pending.EnumerateArray())
            {
                if (entry.TryGetProperty(IdProp, out var id)
                    && entry.TryGetProperty(PathProp, out var path))
                {
                    _pendingPaths[id.GetString()!] = path.Clone();
                }
            }
        }

        if (root.TryGetProperty(IncrementalProp, out var incremental))
        {
            foreach (var element in incremental.EnumerateArray())
            {
                if (!element.TryGetProperty(IdProp, out var idElement))
                {
                    continue;
                }

                var id = idElement.GetString()!;

                if (!_pendingPaths.TryGetValue(id, out var basePath))
                {
                    continue;
                }

                if (element.TryGetProperty(DataProp, out var data))
                {
                    PatchData(basePath, element, JsonObject.Create(data)!);
                }
                else if (element.TryGetProperty(ItemsProp, out var items))
                {
                    PatchItems(basePath, JsonArray.Create(items)!);
                }
            }
        }
    }

    private void PatchData(JsonElement basePath, JsonElement incremental, JsonObject data)
    {
        var current = NavigatePath(_json![DataProp]!, basePath);

        if (incremental.TryGetProperty(SubPathProp, out var subPath))
        {
            current = NavigatePath(current, subPath);
        }

        foreach (var prop in data.ToArray())
        {
            data.Remove(prop.Key);
            current[prop.Key] = prop.Value;
        }
    }

    private void PatchItems(JsonElement basePath, JsonArray items)
    {
        var target = NavigatePath(_json![DataProp]!, basePath).AsArray();

        while (items.Count > 0)
        {
            var item = items[0];
            items.RemoveAt(0);
            target.Add(item);
        }
    }

    private static JsonNode NavigatePath(JsonNode root, JsonElement path)
    {
        var current = root;

        foreach (var segment in path.EnumerateArray())
        {
            current = segment.ValueKind switch
            {
                JsonValueKind.String => current[segment.GetString()!]!,
                JsonValueKind.Number => current[segment.GetInt32()]!,
                _ => throw new NotSupportedException("Path segment must be int or string.")
            };
        }

        return current;
    }
}
