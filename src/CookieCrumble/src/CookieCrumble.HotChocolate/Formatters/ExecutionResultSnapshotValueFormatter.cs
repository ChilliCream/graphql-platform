using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;
using CookieCrumble.Formatters;
using HotChocolate;
using HotChocolate.Execution;

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

    private static async Task FormatStreamAsync(
        IBufferWriter<byte> snapshot,
        IResponseStream stream)
    {
        var docs = new List<JsonDocument>();
        JsonResultPatcher? patcher = null;
        var first = true;

        try
        {
            await foreach (var queryResult in stream.ReadResultsAsync().ConfigureAwait(false))
            {
                if (first)
                {
                    if (queryResult.HasNext ?? false)
                    {
                        var doc = JsonDocument.Parse(queryResult.ToJson());
                        docs.Add(doc);

                        patcher = new JsonResultPatcher();
                        patcher.SetResponse(doc);
                        first = false;
                        continue;
                    }
                    first = false;
                }

                if (patcher is null)
                {
                    snapshot.Append(queryResult.ToJson());
                    snapshot.AppendLine();
                }
                else
                {
                    var doc = JsonDocument.Parse(queryResult.ToJson());
                    docs.Add(doc);

                    patcher.ApplyPatch(doc);
                }
            }

            if (patcher is not null)
            {
                patcher.WriteResponse(snapshot);
                snapshot.AppendLine();
            }
        }
        finally
        {
            foreach (var doc in docs)
            {
                doc.Dispose();
            }
        }
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
    private readonly Dictionary<string, JsonElement> _pendingPaths = new();

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
