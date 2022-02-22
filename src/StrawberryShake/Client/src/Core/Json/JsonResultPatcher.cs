using System;
using System.Collections.Generic;
using System.Linq;
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
        if (_response?.Body is null || response.Body!.RootElement.TryGetProperty(Data, out _))
        {
            throw new NotSupportedException(JsonResultPatcher_NoValidInitialResponse);
        }

        _json ??= JsonObject.Create(_response.Body.RootElement);

        if (_extensions is null && _response.Extensions is not null)
        {
            _extensions = new(_response.Extensions);
        }

        if (_contextData is null && _response.ContextData is not null)
        {
            _contextData = new(_response.ContextData);
        }

        JsonNode current = _json![Data]!;

        if (response.Body is not null &&
            response.Body.RootElement.TryGetProperty(Path, out JsonElement pathProp) &&
            response.Body.RootElement.TryGetProperty(Data, out JsonElement dataProp))
        {
            JsonElement[] path = pathProp.EnumerateArray().ToArray();

            if (path.Length > 1)
            {
                foreach (JsonElement segment in path)
                {
                    current = segment.ValueKind switch
                    {
                        JsonValueKind.String => current[segment.GetString()!]!,
                        JsonValueKind.Number => current[segment.GetInt32()]!,
                        _ => throw new NotSupportedException(
                            JsonResultPatcher_PathSegmentMustBeStringOrInt)
                    };
                }
            }

            JsonElement last = path.Last();

            if (last.ValueKind is JsonValueKind.String)
            {
                current = current[last.GetString()!]!;
                var patchData = JsonObject.Create(dataProp)!;
                foreach ((var key, JsonNode? value) in patchData)
                {
                    current[key] = value;
                }
            }
            else if (last.ValueKind is JsonValueKind.Number)
            {
                var index = last.GetInt32();
                JsonNode? element = current[index];
                var patchData = JsonObject.Create(dataProp)!;

                if (element is null)
                {
                    current[index] = patchData;
                }
                else
                {
                    foreach ((var key, JsonNode? value) in patchData)
                    {
                        element[key] = value;
                    }
                }
            }
            else
            {
                throw new NotSupportedException(JsonResultPatcher_PathSegmentMustBeStringOrInt);
            }
        }

        _extensions = MergeExtensions(response.Extensions, _extensions);
        _contextData = MergeExtensions(response.ContextData, _contextData);

        // TODO : This is inefficient but we want to get the POC working first.
        using var buffer = new ArrayWriter();
        using var writer = new Utf8JsonWriter(buffer);

        _json.WriteTo(writer);
        var json = JsonDocument.Parse(buffer.Body);

        return new(json, response.Exception, false, response.HasNext, _extensions, _contextData);
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
