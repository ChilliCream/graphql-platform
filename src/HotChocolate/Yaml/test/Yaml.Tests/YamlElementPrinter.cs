using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace HotChocolate.Text.Yaml;

internal static class YamlElementPrinter
{
    public static string ToJson(this YamlElement element)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }))
        {
            Write(element, writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Write(YamlElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case YamlValueKind.Object:
                writer.WriteStartObject();

                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    Write(property.Value, writer);
                }

                writer.WriteEndObject();
                break;

            case YamlValueKind.Array:
                writer.WriteStartArray();

                foreach (var item in element.EnumerateArray())
                {
                    Write(item, writer);
                }

                writer.WriteEndArray();
                break;

            case YamlValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case YamlValueKind.Number:
                writer.WriteRawValue(element.ToString());
                break;

            case YamlValueKind.True or YamlValueKind.False:
                writer.WriteBooleanValue(element.GetBoolean());
                break;

            default:
                writer.WriteNullValue();
                break;
        }
    }
}
