using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Text.Json;

namespace HotChocolate.Utilities;

internal static class DictionaryToJsonDocumentConverter
{
    private static readonly JsonWriterOptions s_options = new JsonWriterOptions
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = JsonSerializerOptionDefaults.GraphQL;

    public static JsonElement FromDictionary(IReadOnlyDictionary<string, object?> dictionary)
    {
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, s_options);
        JsonValueFormatter.WriteDictionary(writer, dictionary, s_jsonSerializerOptions, JsonNullIgnoreCondition.None);

        var jsonReader = new Utf8JsonReader(buffer.WrittenSpan);
        return JsonElement.ParseValue(ref jsonReader);
    }

    public static Dictionary<string, object?> ToDictionary(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("JsonElement must be an object.", nameof(element));
        }

        var dictionary = new Dictionary<string, object?>();

        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = ConvertValue(property.Value);
        }

        return dictionary;
    }

    public static JsonElement FromList(IReadOnlyList<object?> list)
    {
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, s_options);
        JsonValueFormatter.WriteValue(writer, list, s_jsonSerializerOptions, JsonNullIgnoreCondition.None);

        var jsonReader = new Utf8JsonReader(buffer.WrittenSpan);
        return JsonElement.ParseValue(ref jsonReader);
    }

    public static List<object?> ToList(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("JsonElement must be an array.", nameof(element));
        }

        var list = new List<object?>();

        foreach (var item in element.EnumerateArray())
        {
            list.Add(ConvertValue(item));
        }

        return list;
    }

    private static object? ConvertValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => ConvertNumber(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => ToDictionary(element),
            JsonValueKind.Array => ToList(element),
            _ => throw new ArgumentException($"Unsupported JsonValueKind: {element.ValueKind}")
        };
    }

    private static object ConvertNumber(JsonElement element)
    {
        // Try to parse as specific numeric types in order of precision
        if (element.TryGetInt32(out var intValue))
        {
            return intValue;
        }

        if (element.TryGetInt64(out var longValue))
        {
            return longValue;
        }

        if (element.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        // Fallback to double for any other numeric values
        return element.GetDouble();
    }
}
