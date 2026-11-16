using System.Text.Json;
using System.Text.Json.Nodes;
using StrawberryShake.Internal;
using static StrawberryShake.Properties.Resources;
using static StrawberryShake.ResultFields;

namespace StrawberryShake.Json;

public class JsonResultPatcher : IResultPatcher<JsonDocument>
{
    private Response<JsonDocument>? _response;
    private JsonObject? _json;
    private Dictionary<string, object?>? _extensions;
    private Dictionary<string, object?>? _contextData;

    public void SetResponse(Response<JsonDocument> response)
    {
        _response = response;
        _json = null;
        _extensions = null;
        _contextData = null;
    }

    public Response<JsonDocument> PatchResponse(Response<JsonDocument> response)
    {
        if (_response?.Body is null)
        {
            throw new NotSupportedException(JsonResultPatcher_NoValidInitialResponse);
        }

        _json ??= JsonObject.Create(_response.Body.RootElement)!;

        if (_extensions is null && _response.Extensions is not null)
        {
            _extensions = new(_response.Extensions);
        }

        if (_contextData is null && _response.ContextData is not null)
        {
            _contextData = new(_response.ContextData);
        }

        if (response.Body is not null)
        {
            var root = response.Body.RootElement;

            if (root.TryGetProperty(ResultFields.Path, out var pathProp)
                && root.TryGetProperty(Data, out var dataProp))
            {
                ApplyPatch(pathProp, dataProp);
            }

            if (root.TryGetProperty(Incremental, out var incremental)
                && incremental.ValueKind is JsonValueKind.Array)
            {
                foreach (var entry in incremental.EnumerateArray())
                {
                    var hasPath = entry.TryGetProperty(ResultFields.Path, out var entryPath);

                    if (hasPath
                        && entry.TryGetProperty(Data, out var entryData)
                        && entryData.ValueKind is JsonValueKind.Object)
                    {
                        ApplyPatch(entryPath, entryData);
                    }

                    if (entry.TryGetProperty(Errors, out var entryErrors)
                        && entryErrors.ValueKind is JsonValueKind.Array)
                    {
                        AddErrors(entryErrors, hasPath ? entryPath : null);
                    }
                }
            }
        }

        _extensions = MergeExtensions(response.Extensions, _extensions);
        _contextData = MergeExtensions(response.ContextData, _contextData);

        // TODO : This is inefficient but we want to get the POC working first.
        using var buffer = new ArrayWriter();
        using var writer = new Utf8JsonWriter(buffer);

        _json.WriteTo(writer);
        writer.Flush();

        var json = JsonDocument.Parse(buffer.GetWrittenMemory());

        return new Response<JsonDocument>(
            json,
            response.Exception,
            false,
            response.HasNext,
            _extensions,
            _contextData);
    }

    private void AddErrors(JsonElement errors, JsonElement? entryPath)
    {
        if (_json![Errors] is not JsonArray target)
        {
            target = [];
            _json[Errors] = target;
        }

        foreach (var error in errors.EnumerateArray())
        {
            var node = JsonNode.Parse(error.GetRawText());

            if (node is JsonObject errorObject
                && entryPath is { ValueKind: JsonValueKind.Array } path
                && !errorObject.ContainsKey(ResultFields.Path))
            {
                errorObject[ResultFields.Path] = JsonNode.Parse(path.GetRawText());
            }

            target.Add(node);
        }
    }

    private void ApplyPatch(JsonElement pathProp, JsonElement dataProp)
    {
        var current = _json![Data]!;

        var path = pathProp.EnumerateArray().ToArray();

        if (path.Length > 1)
        {
            var max = path.Length - 1;

            for (var i = 0; i < max; i++)
            {
                current = path[i].ValueKind switch
                {
                    JsonValueKind.String => current[path[i].GetString()!]!,
                    JsonValueKind.Number => current[path[i].GetInt32()]!,
                    _ => throw new NotSupportedException(
                        JsonResultPatcher_PathSegmentMustBeStringOrInt)
                };
            }
        }

        var last = path[^1];

        if (last.ValueKind is JsonValueKind.String)
        {
            current = current[last.GetString()!]!;
            var patchData = JsonObject.Create(dataProp)!;

            foreach ((var key, var value) in patchData.ToArray())
            {
                patchData.Remove(key);
                current[key] = value;
            }
        }
        else if (last.ValueKind is JsonValueKind.Number)
        {
            var index = last.GetInt32();
            var element = current[index];
            var patchData = JsonObject.Create(dataProp)!;

            if (element is null)
            {
                current[index] = patchData;
            }
            else
            {
                foreach ((var key, var value) in patchData.ToArray())
                {
                    patchData.Remove(key);
                    element[key] = value;
                }
            }
        }
        else
        {
            throw new NotSupportedException(JsonResultPatcher_PathSegmentMustBeStringOrInt);
        }
    }

    private static Dictionary<string, object?>? MergeExtensions(
        IReadOnlyDictionary<string, object?>? source,
        Dictionary<string, object?>? target)
    {
        if (source is null)
        {
            return target;
        }

        if (target is null)
        {
            return new(source);
        }

        foreach (var (key, value) in source)
        {
            target[key] = value;
        }

        return target;
    }
}
