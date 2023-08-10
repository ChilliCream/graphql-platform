using System.Collections;
using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.OpenApi.Helpers;

internal static class Utf8JsonWriterHelper
{

    public static void WriteValueNode(Utf8JsonWriter writer, IValueNode node)
    {
        WriteFieldValue(writer,node);
    }

    private static void WriteFieldValue(
        Utf8JsonWriter writer,
        object? value)
    {
        switch (value)
        {
            case ObjectValueNode objectValue:
                writer.WriteStartObject();

                foreach (var field in objectValue.Fields)
                {
                    writer.WritePropertyName(field.Name.Value);
                    WriteFieldValue(writer, field.Value);
                }

                writer.WriteEndObject();
                break;

            case ListValueNode listValue:
                writer.WriteStartArray();

                foreach (var item in listValue.Items)
                {
                    WriteFieldValue(writer, item);
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

            case Dictionary<string, object?> dict:
                WriteDictionary(writer, dict);
                break;

            case IList list:
                WriteList(writer, list);
                break;

            case string s:
                writer.WriteStringValue(s);
                break;

            case byte b:
                writer.WriteNumberValue(b);
                break;

            case short s:
                writer.WriteNumberValue(s);
                break;

            case ushort s:
                writer.WriteNumberValue(s);
                break;

            case int i:
                writer.WriteNumberValue(i);
                break;

            case uint i:
                writer.WriteNumberValue(i);
                break;


            case double d:
                writer.WriteNumberValue(d);
                break;

            case decimal d:
                writer.WriteNumberValue(d);
                break;

            case bool b:
                writer.WriteBooleanValue(b);
                break;

            case Uri u:
                writer.WriteStringValue(u.ToString());
                break;

            default:
                writer.WriteStringValue(value?.ToString() ?? string.Empty);
                break;
        }
    }

    private static void WriteDictionary(
        Utf8JsonWriter writer,
        Dictionary<string, object?> dict)
    {
        writer.WriteStartObject();

        foreach (var item in dict)
        {
            if (item.Value is null)
            {
                continue;
            }

            writer.WritePropertyName(item.Key);
            WriteFieldValue(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteList(
        Utf8JsonWriter writer,
        IEnumerable list)
    {
        writer.WriteStartArray();

        foreach (var element in list)
        {
            if (element is null)
            {
                continue;
            }

            WriteFieldValue(writer, element);
        }

        writer.WriteEndArray();
    }
}
