using System.Collections;
using System.Text.Json;
using static HotChocolate.Execution.ResultFieldNames;

namespace HotChocolate.Execution;

public static class JsonValueFormatter
{
    public static void WriteValue(
        Utf8JsonWriter writer,
        object? value,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case JsonDocument doc:
                WriteJsonElement(doc.RootElement, writer, options, nullIgnoreCondition);
                break;

            case JsonElement element:
                WriteJsonElement(element, writer, options, nullIgnoreCondition);
                break;

            case RawJsonValue rawJsonValue:
                writer.WriteRawValue(rawJsonValue.Value.Span, true);
                break;

            case IResultDataJsonFormatter formatter:
                formatter.WriteTo(writer, options, nullIgnoreCondition);
                break;

            case Dictionary<string, object?> dict:
                WriteDictionary(writer, dict, options, nullIgnoreCondition);
                break;

            case IReadOnlyDictionary<string, object?> dict:
                WriteDictionary(writer, dict, options, nullIgnoreCondition);
                break;

            case IList list:
                WriteList(writer, list, options, nullIgnoreCondition);
                break;

            case IError error:
                WriteError(writer, error, options, nullIgnoreCondition);
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

            case Path p:
                WritePathValue(writer, p);
                break;

            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    private static void WriteJsonElement(
        JsonElement element,
        Utf8JsonWriter writer,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind is JsonValueKind.Null
                        && (nullIgnoreCondition & JsonNullIgnoreCondition.Fields) == JsonNullIgnoreCondition.Fields)
                    {
                        continue;
                    }

                    writer.WritePropertyName(property.Name);
                    WriteValue(writer, property.Value, options, nullIgnoreCondition);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.Null
                        && (nullIgnoreCondition & JsonNullIgnoreCondition.Lists) == JsonNullIgnoreCondition.Lists)
                    {
                        continue;
                    }

                    WriteValue(writer, item, options, nullIgnoreCondition);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    public static void WriteDictionary(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, object?> dict,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        writer.WriteStartObject();

        foreach (var item in dict)
        {
            if (item.Value is null
                && (nullIgnoreCondition & JsonNullIgnoreCondition.Fields) == JsonNullIgnoreCondition.Fields)
            {
                continue;
            }

            writer.WritePropertyName(item.Key);
            WriteValue(writer, item.Value, options, nullIgnoreCondition);
        }

        writer.WriteEndObject();
    }

    private static void WriteList(
        Utf8JsonWriter writer,
        IList list,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        writer.WriteStartArray();

        for (var i = 0; i < list.Count; i++)
        {
            var element = list[i];

            if (element is null
                && (nullIgnoreCondition & JsonNullIgnoreCondition.Lists) == JsonNullIgnoreCondition.Lists)
            {
                continue;
            }

            WriteValue(writer, element, options, nullIgnoreCondition);
        }

        writer.WriteEndArray();
    }

    private static void WriteDictionary(
        Utf8JsonWriter writer,
        Dictionary<string, object?> dict,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        writer.WriteStartObject();

        foreach (var item in dict)
        {
            if (item.Value is null
                && (nullIgnoreCondition & JsonNullIgnoreCondition.Fields) == JsonNullIgnoreCondition.Fields)
            {
                continue;
            }

            writer.WritePropertyName(item.Key);
            WriteValue(writer, item.Value, options, nullIgnoreCondition);
        }

        writer.WriteEndObject();
    }

    private static void WriteLocations(Utf8JsonWriter writer, IReadOnlyList<Location>? locations)
    {
        if (locations is { Count: > 0 })
        {
            writer.WritePropertyName(Locations);

            writer.WriteStartArray();

            for (var i = 0; i < locations.Count; i++)
            {
                WriteLocation(writer, locations[i]);
            }

            writer.WriteEndArray();
        }
    }

    public static void WriteError(
        Utf8JsonWriter writer,
        IError error,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        writer.WriteStartObject();

        writer.WriteString(Message, error.Message);

        WriteLocations(writer, error.Locations);
        WritePath(writer, error.Path);
        WriteExtensions(writer, error.Extensions, options, nullIgnoreCondition);

        writer.WriteEndObject();
    }

    private static void WriteLocation(Utf8JsonWriter writer, Location location)
    {
        writer.WriteStartObject();
        writer.WriteNumber(Line, location.Line);
        writer.WriteNumber(Column, location.Column);
        writer.WriteEndObject();
    }

    public static void WritePath(Utf8JsonWriter writer, Path? path)
    {
        if (path is not null)
        {
            writer.WritePropertyName(ResultFieldNames.Path);
            WritePathValue(writer, path);
        }
    }

    private static void WritePathValue(Utf8JsonWriter writer, Path path)
    {
        if (path.IsRoot)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        writer.WriteStartArray();

        var list = path.ToList();

        for (var i = 0; i < list.Count; i++)
        {
            switch (list[i])
            {
                case string s:
                    writer.WriteStringValue(s);
                    break;

                case int n:
                    writer.WriteNumberValue(n);
                    break;

                case short n:
                    writer.WriteNumberValue(n);
                    break;

                case long n:
                    writer.WriteNumberValue(n);
                    break;

                default:
                    writer.WriteStringValue(list[i].ToString());
                    break;
            }
        }

        writer.WriteEndArray();
    }

    public static void WriteExtensions(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, object?>? dict,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
    {
        if (dict is { Count: > 0 })
        {
            writer.WritePropertyName(Extensions);
            WriteDictionary(writer, dict, options, nullIgnoreCondition);
        }
    }
}
