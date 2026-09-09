using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotChocolate.Fusion;

/// <summary>
/// Serializes <see cref="NodeResolution"/> using the stable composition settings wire names.
/// <see cref="NodeResolution.Router"/> is persisted as "Gateway" so existing settings documents
/// keep working, and both "Gateway" and "Router" are accepted when reading.
/// </summary>
internal sealed class NodeResolutionJsonConverter : JsonConverter<NodeResolution>
{
    public override NodeResolution Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Number && reader.TryGetInt32(out var number))
        {
            return (NodeResolution)number;
        }

        return reader.GetString() switch
        {
            "Gateway" => NodeResolution.Router,
            "Router" => NodeResolution.Router,
            "SourceSchema" => NodeResolution.SourceSchema,
            var value => throw new JsonException($"Unknown node resolution mode '{value}'.")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        NodeResolution value,
        JsonSerializerOptions options)
    {
        switch (value)
        {
            case NodeResolution.Router:
                writer.WriteStringValue("Gateway");
                break;

            case NodeResolution.SourceSchema:
                writer.WriteStringValue("SourceSchema");
                break;

            default:
                writer.WriteNumberValue((int)value);
                break;
        }
    }
}
