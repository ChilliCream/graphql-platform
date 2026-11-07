using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Fusion;

internal static class GatewaySettingsMerger
{
    public static JsonDocument Merge(
        JsonDocument composed,
        JsonDocument? existing,
        JsonDocument? overlay)
    {
        var result = existing is null
            ? new JsonObject()
            : JsonNode.Parse(existing.RootElement.GetRawText())!.AsObject();

        if (overlay is not null)
        {
            MergeObject(result, JsonNode.Parse(overlay.RootElement.GetRawText())!.AsObject());
        }

        result["sourceSchemas"] = JsonNode.Parse(
            composed.RootElement.GetProperty("sourceSchemas").GetRawText());
        return JsonDocument.Parse(result.ToJsonString());
    }

    private static void MergeObject(JsonObject target, JsonObject overlay)
    {
        foreach (var property in overlay)
        {
            if (property.Value is JsonObject overlayObject
                && target[property.Key] is JsonObject targetObject)
            {
                MergeObject(targetObject, overlayObject);
            }
            else
            {
                target[property.Key] = property.Value?.DeepClone();
            }
        }
    }
}
