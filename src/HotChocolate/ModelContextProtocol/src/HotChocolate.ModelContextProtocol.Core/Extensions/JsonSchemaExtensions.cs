using System.Text.Json;
using Json.Schema;

namespace HotChocolate.ModelContextProtocol.Extensions;

internal static class JsonSchemaExtensions
{
    public static JsonElement ToJsonElement(this JsonSchema jsonSchema)
    {
        var json =
            JsonSerializer.Serialize(
                jsonSchema,
                JsonSchemaJsonSerializerContext.Default.JsonSchema);

        return JsonDocument.Parse(json).RootElement;
    }
}
