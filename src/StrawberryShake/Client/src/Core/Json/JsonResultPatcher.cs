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
        if (_response?.Body is null || !response.Body!.RootElement.TryGetProperty(Data, out _))
        {
            throw new NotSupportedException(JsonResultPatcher_NoValidInitialResponse);
        }

        _json ??= JsonObject.Create(_response.Body.RootElement);

#if NET5_0_OR_GREATER
        if (_extensions is null && _response.Extensions is not null)
        {
            _extensions = new(_response.Extensions);
        }

        if (_contextData is null && _response.ContextData is not null)
        {
            _contextData = new(_response.ContextData);
        }
#else
        if (_extensions is null && _response.Extensions is not null)
        {
            _extensions = _response.Extensions.ToDictionary(t => t.Key, t => t.Value);
        }

        if (_contextData is null && _response.ContextData is not null)
        {
            _contextData = _response.ContextData.ToDictionary(t => t.Key, t => t.Value);
        }
#endif

        var current = _json![Data]!;

        if (response.Body is not null &&
            response.Body.RootElement.TryGetProperty(Path, out var pathProp) &&
            response.Body.RootElement.TryGetProperty(Data, out var dataProp))
        {
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
                            JsonResultPatcher_PathSegmentMustBeStringOrInt),
                    };
                }
            }

#if NET5_0_OR_GREATER
            var last = path[^1];
#else
            var last = path[path.Length - 1];
#endif

            if (last.ValueKind is JsonValueKind.String)
            {
                current = current[last.GetString()!]!;
                var patchData = JsonObject.Create(dataProp)!;

#if NET5_0_OR_GREATER
                foreach ((var key, var value) in patchData.ToArray())
                {
                    patchData.Remove(key);
                    current[key] = value;
                }
#else
                foreach (var prop in patchData.ToArray())
                {
                    patchData.Remove(prop.Key);
                    current[prop.Key] = prop.Value;
                }
#endif
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
#if NET5_0_OR_GREATER
                    foreach ((var key, var value) in patchData.ToArray())
                    {
                        patchData.Remove(key);
                        element[key] = value;
                    }
#else
                    foreach (var prop in patchData.ToArray())
                    {
                        patchData.Remove(prop.Key);
                        element[prop.Key] = prop.Value;
                    }
#endif
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
#if NET5_0_OR_GREATER
            return new(source);
#else
            return source.ToDictionary(t => t.Key, t => t.Value);
#endif
        }

#if NET5_0_OR_GREATER
        foreach (var (key, value) in source)
        {
            target[key] = value;
        }
#else
        foreach (var prop in source)
        {
            target[prop.Key] = prop.Value;
        }
#endif

        return target;
    }
}
