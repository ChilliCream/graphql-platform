using System.Buffers;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Language;

/// <summary>
/// A parser that reads a JSON document as GraphQL value nodes.
/// </summary>
public ref struct JsonValueParser
{
    private const int DefaultMaxAllowedDepth = 64;
    private readonly int _maxAllowedDepth;
    private readonly bool _doNotSeal;
    internal Utf8MemoryBuilder? _memory;
    private readonly PooledArrayWriter? _externalBuffer;

    public JsonValueParser()
    {
        _maxAllowedDepth = DefaultMaxAllowedDepth;
    }

    public JsonValueParser(int maxAllowedDepth)
    {
        _maxAllowedDepth = maxAllowedDepth;
    }

    public JsonValueParser(int maxAllowedDepth, PooledArrayWriter buffer)
    {
        _maxAllowedDepth = maxAllowedDepth;
        _externalBuffer = buffer;
    }

    public JsonValueParser(PooledArrayWriter buffer)
    {
        _maxAllowedDepth = DefaultMaxAllowedDepth;
        _externalBuffer = buffer;
    }

    internal JsonValueParser(bool doNotSeal)
    {
        _maxAllowedDepth = DefaultMaxAllowedDepth;
        _doNotSeal = doNotSeal;
    }

    public IValueNode Parse(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined)
        {
            throw new ArgumentException("Undefined JSON value kind.");
        }

        try
        {
            return Parse(element, 0);
        }
        catch
        {
            _memory?.Abandon();
            _memory = null;
            throw;
        }
        finally
        {
            if (!_doNotSeal)
            {
                _memory?.Seal();
                _memory = null;
            }
        }
    }

    internal IValueNode Parse(JsonElement element, int depth)
    {
        if (depth > _maxAllowedDepth)
        {
            throw new InvalidOperationException("Max allowed depth reached.");
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return NullValueNode.Default;

            case JsonValueKind.True:
                return BooleanValueNode.True;

            case JsonValueKind.False:
                return BooleanValueNode.False;

            case JsonValueKind.String:
            {
                var value = JsonMarshal.GetRawUtf8Value(element);
                value = value.Slice(1, value.Length - 2); // Remove quotes.
                var segment = WriteValue(value);
                return new StringValueNode(null, segment, false);
            }

            case JsonValueKind.Number:
            {
                var value = JsonMarshal.GetRawUtf8Value(element);
                var segment = WriteValue(value);

                if (value.IndexOfAny((byte)'e', (byte)'E') > -1)
                {
                    return new FloatValueNode(segment, FloatFormat.Exponential);
                }

                if (value.IndexOf((byte)'.') > -1)
                {
                    return new FloatValueNode(segment, FloatFormat.FixedPoint);
                }

                return new IntValueNode(segment);
            }

            case JsonValueKind.Array:
            {
                var buffer = ArrayPool<IValueNode>.Shared.Rent(64);
                var count = 0;

                try
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        if (count == buffer.Length)
                        {
                            var temp = buffer;
                            var tempSpan = temp.AsSpan();
                            buffer = ArrayPool<IValueNode>.Shared.Rent(count * 2);
                            tempSpan.CopyTo(buffer);
                            tempSpan.Clear();
                            ArrayPool<IValueNode>.Shared.Return(temp);
                        }

                        buffer[count++] = Parse(item, depth + 1);
                    }

                    return new ListValueNode(buffer.AsSpan(0, count).ToArray());
                }
                finally
                {
                    buffer.AsSpan(0, count).Clear();
                    ArrayPool<IValueNode>.Shared.Return(buffer);
                }
            }

            case JsonValueKind.Object:
            {
                var buffer = ArrayPool<ObjectFieldNode>.Shared.Rent(64);
                var count = 0;

                try
                {
                    foreach (var item in element.EnumerateObject())
                    {
                        if (count == buffer.Length)
                        {
                            var temp = buffer;
                            var tempSpan = temp.AsSpan();
                            buffer = ArrayPool<ObjectFieldNode>.Shared.Rent(count * 2);
                            tempSpan.CopyTo(buffer);
                            tempSpan.Clear();
                            ArrayPool<ObjectFieldNode>.Shared.Return(temp);
                        }

                        buffer[count++] = new ObjectFieldNode(item.Name, Parse(item.Value, depth + 1));
                    }

                    return new ObjectValueNode(buffer.AsSpan(0, count).ToArray());
                }
                finally
                {
                    buffer.AsSpan(0, count).Clear();
                    ArrayPool<ObjectFieldNode>.Shared.Return(buffer);
                }
            }

            default:
                throw new InvalidOperationException("Invalid JSON value kind.");
        }
    }

    /// <summary>
    /// Parses a JSON span as a GraphQL value node.
    /// </summary>
    /// <param name="json">The JSON span to parse.</param>
    /// <returns>The parsed GraphQL value node.</returns>
    public IValueNode Parse(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json, isFinalBlock: true, state: default);
        return Parse(ref reader);
    }

    /// <summary>
    /// Parses a JSON span as a GraphQL value node.
    /// </summary>
    /// <param name="json">The JSON span to parse.</param>
    /// <returns>The parsed GraphQL value node.</returns>
    public IValueNode Parse(ReadOnlySequence<byte> json)
    {
        var reader = new Utf8JsonReader(json, isFinalBlock: true, state: default);
        return Parse(ref reader);
    }

    /// <summary>
    /// Parses a JSON reader as a GraphQL value node.
    /// </summary>
    /// <param name="reader">The JSON reader to parse.</param>
    /// <returns>The parsed GraphQL value node.</returns>
    public IValueNode Parse(ref Utf8JsonReader reader)
    {
        try
        {
            return Parse(ref reader, 0);
        }
        catch
        {
            _memory?.Abandon();
            _memory = null;
            throw;
        }
        finally
        {
            if (!_doNotSeal)
            {
                _memory?.Seal();
                _memory = null;
            }
        }
    }

    private IValueNode Parse(ref Utf8JsonReader reader, int depth, bool skipReading = false)
    {
        if (depth > _maxAllowedDepth)
        {
            throw new InvalidOperationException("Max allowed depth reached.");
        }

        if (!skipReading && !reader.Read())
        {
            throw new JsonException("Unexpected end of JSON.");
        }

        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return NullValueNode.Default;

            case JsonTokenType.True:
                return BooleanValueNode.True;

            case JsonTokenType.False:
                return BooleanValueNode.False;

            case JsonTokenType.String:
            {
                var segment = reader.HasValueSequence
                    ? WriteValue(reader.ValueSequence)
                    : WriteValue(reader.ValueSpan);
                return new StringValueNode(null, segment, false);
            }

            case JsonTokenType.Number:
            {
                var segment = reader.HasValueSequence
                    ? WriteValue(reader.ValueSequence)
                    : WriteValue(reader.ValueSpan);

                if (segment.Span.IndexOfAny((byte)'e', (byte)'E') > -1)
                {
                    return new FloatValueNode(segment, FloatFormat.Exponential);
                }

                if (segment.Span.IndexOf((byte)'.') > -1)
                {
                    return new FloatValueNode(segment, FloatFormat.FixedPoint);
                }

                return new IntValueNode(segment);
            }

            case JsonTokenType.StartArray:
            {
                var buffer = ArrayPool<IValueNode>.Shared.Rent(64);
                var count = 0;

                try
                {
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (count == buffer.Length)
                        {
                            var temp = buffer;
                            var tempSpan = temp.AsSpan();
                            buffer = ArrayPool<IValueNode>.Shared.Rent(count * 2);
                            tempSpan.CopyTo(buffer);
                            tempSpan.Clear();
                            ArrayPool<IValueNode>.Shared.Return(temp);
                        }

                        buffer[count++] = Parse(ref reader, depth + 1, true);
                    }

                    return new ListValueNode(buffer.AsSpan(0, count).ToArray());
                }
                finally
                {
                    buffer.AsSpan(0, count).Clear();
                    ArrayPool<IValueNode>.Shared.Return(buffer);
                }
            }

            case JsonTokenType.StartObject:
            {
                var buffer = ArrayPool<ObjectFieldNode>.Shared.Rent(64);
                var count = 0;

                try
                {
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            throw new JsonException("Expected property name.");
                        }

                        var name = reader.GetString()!;
                        var fieldName = name;

                        if (count == buffer.Length)
                        {
                            var temp = buffer;
                            var tempSpan = temp.AsSpan();
                            buffer = ArrayPool<ObjectFieldNode>.Shared.Rent(count * 2);
                            tempSpan.CopyTo(buffer);
                            tempSpan.Clear();
                            ArrayPool<ObjectFieldNode>.Shared.Return(temp);
                        }

                        buffer[count++] = new ObjectFieldNode(fieldName, Parse(ref reader, depth + 1));
                    }

                    return new ObjectValueNode(buffer.AsSpan(0, count).ToArray());
                }
                finally
                {
                    buffer.AsSpan(0, count).Clear();
                    ArrayPool<ObjectFieldNode>.Shared.Return(buffer);
                }
            }

            default:
                throw new JsonException($"Unsupported token type: {reader.TokenType}");
        }
    }

    private ReadOnlyMemorySegment WriteValue(ReadOnlySequence<byte> value)
    {
        if (_externalBuffer is not null)
        {
            var range = WriteValue(_externalBuffer, value);
            return new ReadOnlyMemorySegment(_externalBuffer, range.Start, range.Length);
        }
        else
        {
            _memory ??= new Utf8MemoryBuilder();
            var range = WriteValue(_memory, value);
            return new ReadOnlyMemorySegment(_memory, range.Start, range.Length);
        }
    }

    private static (int Start, int Length) WriteValue(
        IWritableMemory buffer,
        ReadOnlySequence<byte> value)
    {
        var start = buffer.WrittenSpan.Length;

        foreach (var segment in value)
        {
            var span = buffer.GetSpan(segment.Length);
            segment.Span.CopyTo(span);
            buffer.Advance(segment.Length);
        }

        return (start, buffer.WrittenSpan.Length - start);
    }

    private ReadOnlyMemorySegment WriteValue(ReadOnlySpan<byte> value)
    {
        if (_externalBuffer is not null)
        {
            var start = _externalBuffer.Length;
            _externalBuffer.Write(value);
            return new ReadOnlyMemorySegment(_externalBuffer, start, value.Length);
        }

        _memory ??= new Utf8MemoryBuilder();
        return _memory.Write(value);
    }
}
