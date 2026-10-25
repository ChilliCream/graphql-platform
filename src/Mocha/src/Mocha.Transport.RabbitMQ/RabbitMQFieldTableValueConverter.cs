using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Maps message header values to and from RabbitMQ AMQP field-table values.
/// </summary>
internal static class RabbitMQFieldTableValueConverter
{
    /// <summary>
    /// The greatest depth to which a header value is mapped in either direction.
    /// </summary>
    private const int MaxHeaderDepth = 64;

    /// <summary>
    /// The range a <see cref="DateTimeOffset"/> can express, which bounds the timestamps mapped to one.
    /// </summary>
    private static readonly long s_minTimestampSeconds = DateTimeOffset.MinValue.ToUnixTimeSeconds();

    private static readonly long s_maxTimestampSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

    public static object? ToFieldTableValue(this object? value, string key) => value.ToFieldTableValue(key, 0);

    public static object? FromFieldTableValue(this object? value, string key) => value.FromFieldTableValue(key, 0);

    private static object? ToFieldTableValue(this object? value, string key, int depth)
    {
        EnsureDepth(key, depth);

        switch (value)
        {
            case string:
                return value;

            case DateTimeOffset dateTimeOffset:
                return dateTimeOffset.ToAmqpTimestamp();

            case DateTime dateTime:
                return dateTime.ToAmqpTimestamp();

            // AMQP field tables have no unsigned 64-bit integer type.
            case ulong unsigned when unsigned > long.MaxValue:
                return unsigned.ToString(CultureInfo.InvariantCulture);

            case ulong unsigned:
                return (long)unsigned;

            // AMQP field table decimals are limited to a 32-bit mantissa.
            case decimal number when !FitsTableDecimal(number):
                return number.ToString(CultureInfo.InvariantCulture);

            case char character:
                return character.ToString();

            case Guid guid:
                return guid.ToString();

            case Uri uri:
                return uri.OriginalString;

            case TimeSpan timeSpan:
                return timeSpan.ToString("c", CultureInfo.InvariantCulture);

            case DateOnly date:
                return date.ToString("O", CultureInfo.InvariantCulture);

            case TimeOnly time:
                return time.ToString("O", CultureInfo.InvariantCulture);

            case Enum enumeration:
                return enumeration.ToString();

            case JsonElement element:
                return element.ToFieldTableValue(key, depth);

            case JsonDocument document:
                return document.RootElement.ToFieldTableValue(key, depth);

            case JsonNode node:
                using (var document = JsonSerializer.SerializeToDocument(node))
                {
                    return document.RootElement.ToFieldTableValue(key, depth);
                }

            // BinaryTableValue selects the AMQP byte-array type instead of a long string.
            case byte[] bytes:
                return new BinaryTableValue(bytes);

            case ArraySegment<byte> { Array: null }:
                return new BinaryTableValue([]);

            case ArraySegment<byte> { Offset: 0 } whole when whole.Count == whole.Array.Length:
                return new BinaryTableValue(whole.Array);

            case ArraySegment<byte> segment:
                return new BinaryTableValue(segment.ToArray());

            case ReadOnlyMemory<byte> memory:
                return new BinaryTableValue(memory.ToArray());

            case Memory<byte> writableMemory:
                return new BinaryTableValue(writableMemory.ToArray());

            case IReadOnlyHeaders nested:
                var mappedHeaders = new Dictionary<string, object?>();
                foreach (var header in nested)
                {
                    mappedHeaders[header.Key] = header.Value.ToFieldTableValue(key, depth + 1);
                }

                return mappedHeaders;

            case IDictionary table:
                return table.ToFieldTable(key, depth);

            case IEnumerable sequence:
                var mappedSequence = sequence is ICollection collection ? new List<object?>(collection.Count) : [];
                foreach (var item in sequence)
                {
                    mappedSequence.Add(item.ToFieldTableValue(key, depth + 1));
                }

                return mappedSequence;

            default:
                return value;
        }
    }

    private static object? ToFieldTableValue(this JsonElement element, string key, int depth)
    {
        EnsureDepth(key, depth);

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var table = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    table[property.Name] = property.Value.ToFieldTableValue(key, depth + 1);
                }

                return table;

            case JsonValueKind.Array:
                var items = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    items.Add(item.ToFieldTableValue(key, depth + 1));
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
                    return unsignedValue <= long.MaxValue
                        ? (long)unsignedValue
                        : unsignedValue.ToString(CultureInfo.InvariantCulture);
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

    private static Dictionary<string, object?> ToFieldTable(this IDictionary table, string key, int depth)
    {
        var mapped = new Dictionary<string, object?>(table.Count);

        foreach (DictionaryEntry entry in table)
        {
            // a field table names its entries with text; the client stringifies any other key
            if (entry.Key is not { } entryKey)
            {
                continue;
            }

            var name = entryKey as string ?? entryKey.ToString();

            if (name is null)
            {
                continue;
            }

            mapped[name] = entry.Value.ToFieldTableValue(key, depth + 1);
        }

        return mapped;
    }

    private static object? FromFieldTableValue(this object? value, string key, int depth)
    {
        EnsureDepth(key, depth);

        switch (value)
        {
            // a binary field is kept as bytes without being tested for text
            case BinaryTableValue binary:
                return binary.Bytes;

            // a long string carries text and binary alike, so content is the only signal
            case byte[] bytes:
                return Utf8.IsValid(bytes) ? Encoding.UTF8.GetString(bytes) : bytes;

            // a value outside the range a date can express is kept as the number it is
            case AmqpTimestamp timestamp:
                return timestamp.UnixTime >= s_minTimestampSeconds && timestamp.UnixTime <= s_maxTimestampSeconds
                    ? DateTimeOffset.FromUnixTimeSeconds(timestamp.UnixTime)
                    : timestamp.UnixTime;

            case IDictionary<string, object?> table:
                var mappedTable = new Dictionary<string, object?>(table.Count);
                foreach (var (entryKey, item) in table)
                {
                    mappedTable[entryKey] = item.FromFieldTableValue(key, depth + 1);
                }

                return mappedTable;

            case IList<object?> array:
                var mappedArray = new object?[array.Count];
                for (var i = 0; i < array.Count; i++)
                {
                    mappedArray[i] = array[i].FromFieldTableValue(key, depth + 1);
                }

                return mappedArray;

            default:
                return value;
        }
    }

    private static void EnsureDepth(string key, int depth)
    {
        if (depth > MaxHeaderDepth)
        {
            throw new InvalidOperationException(
                $"The header '{key}' exceeds the maximum AMQP field-table nesting depth of {MaxHeaderDepth}.");
        }
    }

    private static bool FitsTableDecimal(decimal value)
    {
        // decimal stores a 96-bit mantissa in the first three words, while an AMQP field-table
        // decimal has only a signed 32-bit mantissa.
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);

        return bits[1] == 0 && bits[2] == 0 && (uint)bits[0] <= int.MaxValue;
    }
}
