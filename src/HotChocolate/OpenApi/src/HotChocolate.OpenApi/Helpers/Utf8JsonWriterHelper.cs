using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.OpenApi.Helpers;

internal static class Utf8JsonWriterHelper
{
    public static void WriteValueNode(Utf8JsonWriter writer, IValueNode value)
    {
        switch (value)
        {
            case BooleanValueNode booleanValue:
                writer.WriteBooleanValue(booleanValue.Value);
                break;

            case EnumValueNode enumValue:
                writer.WriteStringValue(enumValue.Value);
                break;

            case FloatValueNode floatValue:
                writer.WriteRawValue(floatValue.Value);
                break;

            case IntValueNode intValue:
                writer.WriteRawValue(intValue.Value);
                break;

            case ListValueNode listValue:
                writer.WriteStartArray();

                foreach (var item in listValue.Items)
                {
                    WriteValueNode(writer, item);
                }

                writer.WriteEndArray();
                break;

            case ObjectValueNode objectValue:
                writer.WriteStartObject();

                foreach (var field in objectValue.Fields)
                {
                    writer.WritePropertyName(field.Name.Value);
                    WriteValueNode(writer, field.Value);
                }

                writer.WriteEndObject();
                break;

            case StringValueNode stringValue:
                writer.WriteStringValue(stringValue.Value);
                break;

            default:
                throw new NotSupportedException();
        }
    }
}
