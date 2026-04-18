using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocha;

/// <summary>
/// JSON converter for serializing and deserializing <see cref="IHeaders"/> instances as JSON objects with dynamic value types.
/// </summary>
public class HeadersJsonConverter : JsonConverter<IHeaders>
{
    /// <summary>
    /// Gets a shared singleton instance of the converter.
    /// </summary>
    public static readonly HeadersJsonConverter Instance = new();

    /// <summary>
    /// Gets pre-configured <see cref="JsonSerializerOptions"/> with this converter registered.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new() { Converters = { Instance } };

    /// <inheritdoc />
    public override Headers? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject token, got {reader.TokenType}");
        }

        var headers = new Headers();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return headers;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected PropertyName token, got {reader.TokenType}");
            }

            var key = reader.GetString()!;
            reader.Read();

            var value = ReadValue(ref reader, options);
            headers.Set(key, value);
        }

        throw new JsonException("Unexpected end of JSON");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IHeaders value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var header in value)
        {
            writer.WritePropertyName(header.Key);
            WriteValue(writer, header.Value, options);
        }

        writer.WriteEndObject();
    }

    private static object? ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.String:
                if (reader.TryGetDateTime(out var dateTime))
                {
                    return dateTime;
                }
                return reader.GetString();

            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var intValue))
                {
                    return intValue;
                }
                if (reader.TryGetInt64(out var longValue))
                {
                    return longValue;
                }
                if (reader.TryGetDouble(out var doubleValue))
                {
                    return doubleValue;
                }
                throw new JsonException("Unable to parse number");

            case JsonTokenType.StartObject:
                return ReadObject(ref reader, options);

            case JsonTokenType.StartArray:
                return ReadArray(ref reader, options);

            default:
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
        }
    }

    private static Dictionary<string, object?> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var dictionary = new Dictionary<string, object?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected PropertyName token, got {reader.TokenType}");
            }

            var key = reader.GetString()!;
            reader.Read();

            dictionary[key] = ReadValue(ref reader, options);
        }

        throw new JsonException("Unexpected end of JSON in object");
    }

    private static object?[] ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var list = new List<object?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return list.ToArray();
            }

            list.Add(ReadValue(ref reader, options));
        }

        throw new JsonException("Unexpected end of JSON in array");
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;

            case bool boolValue:
                writer.WriteBooleanValue(boolValue);
                break;

            case string stringValue:
                writer.WriteStringValue(stringValue);
                break;

            case int intValue:
                writer.WriteNumberValue(intValue);
                break;

            case long longValue:
                writer.WriteNumberValue(longValue);
                break;

            case double doubleValue:
                writer.WriteNumberValue(doubleValue);
                break;

            case float floatValue:
                writer.WriteNumberValue(floatValue);
                break;

            case decimal decimalValue:
                writer.WriteNumberValue(decimalValue);
                break;

            case DateTime dateTimeValue:
                writer.WriteStringValue(dateTimeValue);
                break;

            case DateTimeOffset dateTimeOffsetValue:
                writer.WriteStringValue(dateTimeOffsetValue);
                break;

            case JsonElement jsonElement:
                jsonElement.WriteTo(writer);
                break;

            case JsonDocument jsonDocument:
                jsonDocument.RootElement.WriteTo(writer);
                break;

            case IDictionary<string, object?> dictionary:
                writer.WriteStartObject();
                foreach (var kvp in dictionary)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteValue(writer, kvp.Value, options);
                }
                writer.WriteEndObject();
                break;

            case IEnumerable<object?> enumerable:
                writer.WriteStartArray();
                foreach (var item in enumerable)
                {
                    WriteValue(writer, item, options);
                }
                writer.WriteEndArray();
                break;

            case IReadOnlyHeaders headers:
                writer.WriteStartObject();
                foreach (var header in headers)
                {
                    writer.WritePropertyName(header.Key);
                    WriteValue(writer, header.Value, options);
                }
                writer.WriteEndObject();
                break;

            case Guid g:
                writer.WriteStringValue(g);
                break;

            case TimeSpan t:
                writer.WriteStringValue(t.ToString("c", CultureInfo.InvariantCulture));
                break;

            case Uri u:
                writer.WriteStringValue(u.ToString());
                break;

            case DateOnly d:
                writer.WriteStringValue(d.ToString("O", CultureInfo.InvariantCulture));
                break;

            case TimeOnly to:
                writer.WriteStringValue(to.ToString("O", CultureInfo.InvariantCulture));
                break;

            case Enum e:
                writer.WriteStringValue(e.ToString());
                break;

            case short s:
                writer.WriteNumberValue(s);
                break;

            case ushort us:
                writer.WriteNumberValue(us);
                break;

            case byte b:
                writer.WriteNumberValue(b);
                break;

            case sbyte sb:
                writer.WriteNumberValue(sb);
                break;

            case uint ui:
                writer.WriteNumberValue(ui);
                break;

            case ulong ul:
                writer.WriteNumberValue(ul);
                break;

            case char c:
                writer.WriteStringValue([c]);
                break;

            default:
                throw new InvalidOperationException(
                    $"Header value type '{value.GetType().Name}' is not supported for serialization. "
                    + "Supported: string, bool, numeric primitives, char, DateTime, DateTimeOffset, "
                    + "DateOnly, TimeOnly, TimeSpan, Guid, Uri, Enum, JsonElement, JsonDocument, "
                    + "IDictionary<string, object?>, IEnumerable<object?>, IReadOnlyHeaders. "
                    + "Custom types must be stringified by the caller before assignment.");
        }
    }
}
