using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotChocolate.AspNetCore.Authorization.Opa;

/// <summary>
/// Opa Result Converter
/// </summary>
/// <remarks>
/// As described in https://www.openpolicyagent.org/docs/latest/rest-api/#get-a-document
/// The server returns 200 if the path refers to an undefined document.
/// In this case, the response will not contain a result property.
/// The property is actually returned as an empty object '{ }'.
/// Therefore, it can't be deserialized as nullable boolean by default, hence this converter.
/// </remarks>
public class OpaResultFieldConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) return reader.GetBoolean();
        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value is { } v) writer.WriteBooleanValue(v);
    }
}
