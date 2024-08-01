using System.Collections;
using System.Text;
using System.Text.Json;
using StrawberryShake.Internal;

namespace StrawberryShake.Json;

public static class JsonSerializationHelper
{
    public static string WriteValue(object? value)
    {
        using var arrayWriter = new ArrayWriter();
        using var jsonWriter = new Utf8JsonWriter(arrayWriter);
        WriteValue(value, jsonWriter);
        jsonWriter.Flush();
        return Encoding.UTF8.GetString(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
    }

    public static void WriteValue(object? value, Utf8JsonWriter writer)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case JsonElement element:
                element.WriteTo(writer);
                break;

            case byte[] b:
                writer.WriteBase64StringValue(b);
                break;

            case IEnumerable<KeyValuePair<string, object?>> dict:
                WriteDictionary(dict, writer);
                break;

            case IList list:
                WriteList(list, writer);
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

            case long l:
                writer.WriteNumberValue(l);
                break;

            case ulong l:
                writer.WriteNumberValue(l);
                break;

            case float f:
                writer.WriteNumberValue(f);
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
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    public static void WriteDictionary(
        IEnumerable<KeyValuePair<string, object?>> dictionary,
        Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        foreach (var property in dictionary)
        {
            writer.WritePropertyName(property.Key);
            WriteValue(property.Value, writer);
        }

        writer.WriteEndObject();
    }

    public static void WriteList(IList list, Utf8JsonWriter writer)
    {
        writer.WriteStartArray();

        foreach (var element in list)
        {
            WriteValue(element, writer);
        }

        writer.WriteEndArray();
    }

    public static object? ReadValue(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ReadValue(document.RootElement);
    }

    public static object? ReadValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return null;

            case JsonValueKind.Object:
                return ReadDictionary(element);

            case JsonValueKind.Array:
                return ReadList(element);

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var integerValue))
                {
                    return integerValue;
                }

                if (element.TryGetDouble(out var floatValue))
                {
                    return floatValue;
                }

                throw new NotSupportedException();

            case JsonValueKind.False:
                return false;

            case JsonValueKind.True:
                return true;

            default:
                throw new NotSupportedException();
        }
    }

    public static Dictionary<string, object?> ReadDictionary(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ReadDictionary(document.RootElement);
    }

    public static Dictionary<string, object?> ReadDictionary(
        JsonElement element)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ReadValue(property.Value);
        }

        return dict;
    }

    public static List<object?> ReadList(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ReadList(document.RootElement);
    }

    public static List<object?> ReadList(JsonElement element)
    {
        var list = new List<object?>();

        foreach (var item in element.EnumerateArray())
        {
            list.Add(ReadValue(item));
        }

        return list;
    }
}
