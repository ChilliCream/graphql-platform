using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Mocha;

/// <summary>
/// JSON converter for serializing and deserializing <see cref="IHeaders"/> instances as JSON objects with dynamic value types.
/// </summary>
/// <remarks>
/// <para>
/// Header values are weakly typed, but the set of types that can be written is closed:
/// <see langword="null" />, <see cref="bool" />, <see cref="string" />,
/// <see cref="char" />, the numeric types, <see cref="DateTime" />,
/// <see cref="DateTimeOffset" />, <see cref="DateOnly" />, <see cref="TimeOnly" />,
/// <see cref="TimeSpan" />, <see cref="Guid" />, <see cref="Uri" />, an enum, a byte payload
/// (written as base64), a JSON value (<see cref="JsonElement" />, <see cref="JsonDocument" />,
/// <see cref="JsonNode" />), nested headers, a dictionary with string keys, or a sequence of any of
/// these. Through <see cref="Options" />, any other type is rejected with a
/// <see cref="JsonException" /> naming the header; through options carrying a reflection-based
/// resolver, that resolver serializes the value instead.
/// </para>
/// <para>
/// Reading produces a narrower set, because JSON carries no type tag:
/// <see langword="null" />, <see cref="bool" />, <see cref="string" />, <see cref="int" />,
/// <see cref="long" />, <see cref="ulong" />, <see cref="double" />, a dictionary, or an array, so a
/// value keeps its JSON representation across a round trip but not always its CLR type. A
/// <see cref="Guid" />, <see cref="Uri" />, <see cref="TimeSpan" />, <see cref="DateTime" /> or an
/// enum reads back as the string written, a byte payload as base64 text, a sequence as an array.
/// </para>
/// <para>
/// A number is read by value, not by the type that wrote it, taking the narrowest of
/// <see cref="int" />, <see cref="long" /> and <see cref="ulong" /> that holds it: a
/// <see cref="long" />, or a whole <see cref="double" />, <see cref="float" /> or
/// <see cref="decimal" />, can come back as an <see cref="int" />. A fractional value reads as a
/// <see cref="double" />, losing <see cref="decimal" /> precision, and a whole number beyond
/// <see cref="ulong" /> reads as a <see cref="double" /> written again in scientific notation.
/// </para>
/// </remarks>
public class HeadersJsonConverter : JsonConverter<IHeaders>
{
    /// <summary>
    /// Gets a shared singleton instance of the converter.
    /// </summary>
    public static readonly HeadersJsonConverter Instance = new();

    /// <summary>
    /// Gets pre-configured <see cref="JsonSerializerOptions"/> with this converter registered.
    /// </summary>
    /// <remarks>
    /// These options resolve metadata for <see cref="IHeaders"/> and <see cref="Headers"/> only, and
    /// are read-only.
    /// </remarks>
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { Instance },

            // left unset, the serializer installs the reflection-based resolver on first use
            TypeInfoResolver = new HeadersTypeInfoResolver()
        };

        // the writer and reader use the converter directly, so nothing else seals these
        options.MakeReadOnly();

        return options;
    }

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
            WriteValue(writer, header.Value, options, header.Key);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads a JSON value into the types a header holds, the reading <see cref="Read" /> applies to a
    /// document.
    /// </summary>
    /// <param name="element">The JSON value to read.</param>
    /// <returns>The value mapped onto the types a header can hold.</returns>
    internal static object? ReadHeaderValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var table = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    table[property.Name] = ReadHeaderValue(property.Value);
                }

                return table;

            case JsonValueKind.Array:
                var items = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    items.Add(ReadHeaderValue(item));
                }

                return items;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                {
                    return intValue;
                }
                if (element.TryGetInt64(out var longValue))
                {
                    return longValue;
                }
                if (element.TryGetUInt64(out var unsignedValue))
                {
                    return unsignedValue;
                }

                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            default:
                return null;
        }
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

            // JSON carries no type tag, so text stays text
            case JsonTokenType.String:
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
                if (reader.TryGetUInt64(out var ulongValue))
                {
                    return ulongValue;
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

    private static void WriteValue(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, string key)
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

            case byte[] bytesValue:
                writer.WriteBase64StringValue(bytesValue);
                break;

            case ArraySegment<byte> bytesSegment:
                writer.WriteBase64StringValue(bytesSegment);
                break;

            case ReadOnlyMemory<byte> bytesMemory:
                writer.WriteBase64StringValue(bytesMemory.Span);
                break;

            case Memory<byte> writableBytesMemory:
                writer.WriteBase64StringValue(writableBytesMemory.Span);
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

            // must precede the collection cases
            case JsonNode jsonNode:
                jsonNode.WriteTo(writer);
                break;

            case IDictionary<string, object?> dictionary:
                writer.WriteStartObject();
                foreach (var kvp in dictionary)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteValue(writer, kvp.Value, options, key);
                }
                writer.WriteEndObject();
                break;

            case IReadOnlyHeaders headers:
                writer.WriteStartObject();
                foreach (var header in headers)
                {
                    writer.WritePropertyName(header.Key);
                    WriteValue(writer, header.Value, options, key);
                }
                writer.WriteEndObject();
                break;

            // the writer's own overload formats into the buffer instead of allocating a string
            case Guid g:
                writer.WriteStringValue(g);
                break;

            // the text forms every transport shares
            case TimeSpan t:
                writer.WriteStringValue(HeaderValueText.From(t));
                break;

            case Uri u:
                writer.WriteStringValue(HeaderValueText.From(u));
                break;

            case DateOnly d:
                writer.WriteStringValue(HeaderValueText.From(d));
                break;

            case TimeOnly to:
                writer.WriteStringValue(HeaderValueText.From(to));
                break;

            case Enum e:
                writer.WriteStringValue(HeaderValueText.From(e));
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

            // int[] and Dictionary<string, string> have no covariant conversion to object? items
            case IDictionary dictionary:
                WriteDictionary(writer, dictionary, options, key);
                break;

            case IEnumerable enumerable:
                writer.WriteStartArray();
                foreach (var item in enumerable)
                {
                    WriteValue(writer, item, options, key);
                }
                writer.WriteEndArray();
                break;

            default:
                WriteUnknownValue(writer, value, options, key);
                break;
        }
    }

    private static void WriteDictionary(
        Utf8JsonWriter writer,
        IDictionary dictionary,
        JsonSerializerOptions options,
        string key)
    {
        writer.WriteStartObject();

        foreach (DictionaryEntry entry in dictionary)
        {
            // a custom dictionary can hand back any key, a null included
            if (entry.Key is not string name)
            {
                var keyType = entry.Key is { } entryKey ? entryKey.GetType().FullName : "null";

                throw new JsonException(
                    $"The header '{key}' holds a dictionary keyed by '{keyType}' that cannot be "
                        + "written as JSON. Use string keys.");
            }

            writer.WritePropertyName(name);
            WriteValue(writer, entry.Value, options, key);
        }

        writer.WriteEndObject();
    }

    private static void WriteUnknownValue(
        Utf8JsonWriter writer,
        object value,
        JsonSerializerOptions options,
        string key)
    {
        try
        {
            JsonSerializer.Serialize(writer, value, options.GetTypeInfo(value.GetType()));
        }
        catch (NotSupportedException ex)
        {
            throw new JsonException(
                $"The header '{key}' holds a value of type '{value.GetType().FullName}' that cannot be "
                    + "written as JSON. Set the header to a supported value type, or register JSON type "
                    + "metadata for it.",
                ex);
        }
    }

    /// <summary>
    /// Resolves JSON type metadata for headers, and for nothing else.
    /// </summary>
    private sealed class HeadersTypeInfoResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            if (type == typeof(IHeaders))
            {
                return JsonMetadataServices.CreateValueInfo<IHeaders>(options, Instance);
            }

            // the concrete type Read returns
            if (type == typeof(Headers))
            {
                return JsonMetadataServices.CreateValueInfo<Headers>(options, Instance);
            }

            return null;
        }
    }
}
