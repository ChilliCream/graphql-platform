using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.OpenApi.Helpers;

internal static class Utf8JsonWriterHelper
{
    public static void WriteValueNode(
        Utf8JsonWriter writer,
        IValueNode value)
    {
        switch (value)
        {
            case ObjectValueNode objectValue:
                writer.WriteStartObject();

                for (var i = 0; i < objectValue.Fields.Count; i++)
                {
                    var field = objectValue.Fields[i];
                    writer.WritePropertyName(field.Name.Value);
                    WriteValueNode(writer, field.Value);
                }

                writer.WriteEndObject();
                break;

            case ListValueNode listValue:
                writer.WriteStartArray();

                for (var i = 0; i < listValue.Items.Count; i++)
                {
                    var item = listValue.Items[i];
                    WriteValueNode(writer, item);
                }

                writer.WriteEndArray();
                break;

            case StringValueNode stringValue:
                writer.WriteStringValue(stringValue.Value);
                break;

            case IntValueNode intValue:
                writer.WriteRawValue(intValue.Value);
                break;

            case FloatValueNode floatValue:
                writer.WriteRawValue(floatValue.Value);
                break;

            case BooleanValueNode booleanValue:
                writer.WriteBooleanValue(booleanValue.Value);
                break;

            case EnumValueNode enumValue:
                writer.WriteStringValue(enumValue.Value);
                break;

            default:
                throw new NotSupportedException();
        }
    }
}
