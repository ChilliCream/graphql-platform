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
    private const string _data = "data";
    private const string _items = "items";
    private const string _incremental = "incremental";
    private const string _path = "path";
    private JsonObject? _json;

    public void SetResponse(JsonDocument response)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        _json = JsonObject.Create(response.RootElement);
    }

    public void WriteResponse(IBufferWriter<byte> snapshot)
    {
        if (_json is null)
        {
            throw new InvalidOperationException(
                "You must first set the initial response before you can apply patches.");
        }

        using var writer = new Utf8JsonWriter(snapshot, new JsonWriterOptions { Indented = true, });

        _json.Remove("hasNext");

        _json.WriteTo(writer);
        writer.Flush();
    }

    public void ApplyPatch(JsonDocument patch)
    {
        if (_json is null)
        {
            throw new InvalidOperationException(
                "You must first set the initial response before you can apply patches.");
        }

        if (!patch.RootElement.TryGetProperty(_incremental, out var incremental))
        {
            throw new ArgumentException("A patch result must contain a property `incremental`.");
        }

        foreach (var element in incremental.EnumerateArray())
        {
            if (element.TryGetProperty(_data, out var data))
            {
                PatchIncrementalData(element, JsonObject.Create(data)!);
            }
            else if (element.TryGetProperty(_items, out var items))
            {
                PatchIncrementalItems(element, JsonArray.Create(items)!);
            }
        }
    }

    private void PatchIncrementalData(JsonElement incremental, JsonObject data)
    {
        if (incremental.TryGetProperty(_path, out var pathProp))
        {
            var (current, last) = SelectNodeToPatch(_json![_data]!, pathProp);
            ApplyPatch(current, last, data);
        }
    }

    private void PatchIncrementalItems(JsonElement incremental, JsonArray items)
    {
        if (incremental.TryGetProperty(_path, out var pathProp))
        {
            var (current, last) = SelectNodeToPatch(_json![_data]!, pathProp);
            var i = last.GetInt32();
            var target = current.AsArray();

            while (items.Count > 0)
            {
                var item = items[0];
                items.RemoveAt(0);
                target.Insert(i++, item);
            }
        }
    }

    private static void ApplyPatch(JsonNode current, JsonElement last, JsonObject patchData)
    {
        if (last.ValueKind is JsonValueKind.Undefined)
        {
            foreach (var prop in patchData.ToArray())
            {
                patchData.Remove(prop.Key);
                current[prop.Key] = prop.Value;
            }
        }
        else if (last.ValueKind is JsonValueKind.String)
        {
            current = current[last.GetString()!]!;

            foreach (var prop in patchData.ToArray())
            {
                patchData.Remove(prop.Key);
                current[prop.Key] = prop.Value;
            }
        }
        else if (last.ValueKind is JsonValueKind.Number)
        {
            var index = last.GetInt32();
            var element = current[index];

            if (element is null)
            {
                current[index] = patchData;
            }
            else
            {
                foreach (var prop in patchData.ToArray())
                {
                    patchData.Remove(prop.Key);
                    element[prop.Key] = prop.Value;
                }
            }
        }
        else
        {
            throw new NotSupportedException("Path segment must be int or string.");
        }
    }

    private static (JsonNode Node, JsonElement PathSegment) SelectNodeToPatch(
        JsonNode root,
        JsonElement path)
    {
        if (path.GetArrayLength() == 0)
        {
            return (root, default);
        }

        var current = root;
        JsonElement? last = null;

        foreach (var element in path.EnumerateArray())
        {
            if (last is not null)
            {
                current = last.Value.ValueKind switch
                {
                    JsonValueKind.String => current[last.Value.GetString()!]!,
                    JsonValueKind.Number => current[last.Value.GetInt32()]!,
                    _ => throw new NotSupportedException("Path segment must be int or string."),
                };
            }

            last = element;
        }

        if (current is null || last is null)
        {
            throw new InvalidOperationException("Patch had invalid structure.");
        }

        return (current, last.Value);
    }
}
