#nullable enable
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Types.Relay;

/// <summary>
/// A base class for composite node id serializers.
/// </summary>
/// <typeparam name="T">
/// The type of the value that is being serialized.
/// </typeparam>
public abstract class CompositeNodeIdValueSerializer<T> : INodeIdValueSerializer
{
    private const byte PartSeparator = (byte)':';
    private const byte Escape = (byte)'\\';
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    public virtual bool IsSupported(Type type) => type == typeof(T) || type == typeof(T?);

    /// <summary>
    /// Formats the value into the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to write the value into.
    /// </param>
    /// <param name="value">
    /// The value that shall be written into the buffer.
    /// </param>
    /// <param name="written">
    /// The number of bytes that have been written into the buffer.
    /// </param>
    /// <returns>
    /// Returns the result of the formatting operation.
    /// </returns>
    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is T t)
        {
            var result = Format(buffer, t, out written);

            if (result == NodeIdFormatterResult.Success && buffer[written - 1] == PartSeparator)
            {
                written--;
            }

            return result;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    /// <summary>
    /// Formats the value into the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to write the value into.
    /// </param>
    /// <param name="value">
    /// The value that shall be written into the buffer.
    /// </param>
    /// <param name="written">
    /// The number of bytes that have been written into the buffer.
    /// </param>
    /// <returns>
    /// Returns the result of the formatting operation.
    /// </returns>
    protected abstract NodeIdFormatterResult Format(Span<byte> buffer, T value, out int written);

    /// <summary>
    /// Tries to format a string value into the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to write the value into.
    /// </param>
    /// <param name="value">
    /// The value that shall be written into the buffer.
    /// </param>
    /// <param name="written">
    /// The number of bytes that have been written into the buffer.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be written into the buffer.
    /// </returns>
    protected static bool TryFormatIdPart(Span<byte> buffer, string value, out int written)
    {
        var requiredCapacity = s_utf8.GetByteCount(value) * 2 + 1; // * 2 to allow for escaping.
        if (buffer.Length < requiredCapacity)
        {
            written = 0;
            return false;
        }

        Span<byte> utf8Bytes = stackalloc byte[s_utf8.GetByteCount(value)];
        s_utf8.GetBytes(value, utf8Bytes);

        var bytesWritten = WriteEscapedBytes(utf8Bytes, buffer);

        buffer = buffer[bytesWritten..];
        buffer[0] = PartSeparator;
        written = bytesWritten + 1;
        return true;
    }

    /// <summary>
    /// Tries to format a guid value into the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to write the value into.
    /// </param>
    /// <param name="value">
    /// The value that shall be written into the buffer.
    /// </param>
    /// <param name="written">
    /// The number of bytes that have been written into the buffer.
    /// </param>
    /// <param name="compress">
    /// Defines if the guid shall be compressed.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be written into the buffer.
    /// </returns>
    protected static bool TryFormatIdPart(Span<byte> buffer, Guid value, out int written, bool compress = true)
    {
        if (compress)
        {
            const int requiredCapacity = 16 * 2 + 1; // * 2 to allow for escaping.
            if (buffer.Length < requiredCapacity)
            {
                written = 0;
                return false;
            }

            Span<byte> span = stackalloc byte[16];
#pragma warning disable CS9191
            MemoryMarshal.TryWrite(span, ref value);
#pragma warning restore CS9191
            var bytesWritten = WriteEscapedBytes(span, buffer);

            buffer = buffer[bytesWritten..];
            buffer[0] = PartSeparator;
            written = bytesWritten + 1;
            return true;
        }

        if (Utf8Formatter.TryFormat(value, buffer, out written, format: 'N'))
        {
            buffer = buffer[written..];
            if (buffer.Length < 1)
            {
                return false;
            }

            buffer[0] = PartSeparator;
            written++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to format a short value into the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to write the value into.
    /// </param>
    /// <param name="value">
    /// The value that shall be written into the buffer.
    /// </param>
    /// <param name="written">
    /// The number of bytes that have been written into the buffer.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be written into the buffer.
    /// </returns>
    protected static bool TryFormatIdPart(Span<byte> buffer, short value, out int written)
    {
        if (!Utf8Formatter.TryFormat(value, buffer, out written))
        {
            return false;
        }

        buffer = buffer[written..];
        if (buffer.Length < 1)
        {
            return false;
        }

        buffer[0] = PartSeparator;
        written++;
        return true;
    }

    /// <summary>
    /// Tries to format a int value into the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to write the value into.
    /// </param>
    /// <param name="value">
    /// The value that shall be written into the buffer.
    /// </param>
    /// <param name="written">
    /// The number of bytes that have been written into the buffer.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be written into the buffer.
    /// </returns>
    protected static bool TryFormatIdPart(Span<byte> buffer, int value, out int written)
    {
        if (!Utf8Formatter.TryFormat(value, buffer, out written))
        {
            return false;
        }

        buffer = buffer[written..];
        if (buffer.Length < 1)
        {
            return false;
        }

        buffer[0] = PartSeparator;
        written++;
        return true;
    }

    /// <summary>
    /// Tries to format a long value into the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to write the value into.
    /// </param>
    /// <param name="value">
    /// The value that shall be written into the buffer.
    /// </param>
    /// <param name="written">
    /// The number of bytes that have been written into the buffer.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be written into the buffer.
    /// </returns>
    protected static bool TryFormatIdPart(Span<byte> buffer, long value, out int written)
    {
        if (!Utf8Formatter.TryFormat(value, buffer, out written))
        {
            return false;
        }

        buffer = buffer[written..];
        if (buffer.Length < 1)
        {
            return false;
        }

        buffer[0] = PartSeparator;
        written++;
        return true;
    }

    /// <summary>
    /// Tries to format a boolean value into the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to write the value into.
    /// </param>
    /// <param name="value">
    /// The value that shall be written into the buffer.
    /// </param>
    /// <param name="written">
    /// The number of bytes that have been written into the buffer.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be written into the buffer.
    /// </returns>
    protected static bool TryFormatIdPart(Span<byte> buffer, bool value, out int written)
    {
        if (buffer.Length < 2)
        {
            written = 0;
            return false;
        }

        buffer[0] = (byte)(value ? '1' : '0');
        buffer[1] = PartSeparator;
        written = 2;
        return true;
    }

    /// <summary>
    /// Tries to parse the value from the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to read the value from.
    /// </param>
    /// <param name="value">
    /// The parsed value.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be parsed.
    /// </returns>
    public bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
    {
        if (TryParse(buffer, out T? parsedValue))
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Tries to parse the value from the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to read the value from.
    /// </param>
    /// <param name="value">
    /// The parsed value.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be parsed.
    /// </returns>
    protected abstract bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T? value);

    /// <summary>
    /// Tries to parse a string value from the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to read the value from.
    /// </param>
    /// <param name="value">
    /// The parsed value.
    /// </param>
    /// <param name="consumed">
    /// The number of bytes that have been consumed.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be parsed.
    /// </returns>
    protected static unsafe bool TryParseIdPart(
        ReadOnlySpan<byte> buffer,
        [NotNullWhen(true)] out string? value,
        out int consumed)
    {
        var index = IndexOfPartSeparator(buffer);
        var valueSpan = index == -1 ? buffer : buffer[..index];
        valueSpan = Unescape(valueSpan);
        fixed (byte* b = valueSpan)
        {
            value = s_utf8.GetString(b, valueSpan.Length);
        }

        consumed = index + 1;
        return true;
    }

    /// <summary>
    /// Tries to parse a guid value from the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to read the value from.
    /// </param>
    /// <param name="value">
    /// The parsed value.
    /// </param>
    /// <param name="consumed">
    /// The number of bytes that have been consumed.
    /// </param>
    /// <param name="compress">
    /// Defines if the guid shall be compressed.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be parsed.
    /// </returns>
    protected static bool TryParseIdPart(
        ReadOnlySpan<byte> buffer,
        out Guid value,
        out int consumed,
        bool compress = true)
    {
        var index = IndexOfPartSeparator(buffer);
        var valueSpan = index == -1 ? buffer : buffer[..index];

        if (compress)
        {
            valueSpan = Unescape(valueSpan);

            if (valueSpan.Length != 16)
            {
                value = Guid.Empty;
                consumed = 0;
                return false;
            }

            value = new Guid(valueSpan);
            consumed = index + 1;
            return true;
        }

        if (Utf8Parser.TryParse(valueSpan, out Guid parsedValue, out _, standardFormat: 'N'))
        {
            value = parsedValue;
            consumed = index + 1;
            return true;
        }

        value = Guid.Empty;
        consumed = 0;
        return false;
    }

    /// <summary>
    /// Tries to parse a short value from the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to read the value from.
    /// </param>
    /// <param name="value">
    /// The parsed value.
    /// </param>
    /// <param name="consumed">
    /// The number of bytes that have been consumed.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be parsed.
    /// </returns>
    protected static bool TryParseIdPart(
        ReadOnlySpan<byte> buffer,
        out short value,
        out int consumed)
    {
        var index = buffer.IndexOf(PartSeparator);
        var valueSpan = index == -1 ? buffer : buffer[..index];

        if (Utf8Parser.TryParse(valueSpan, out short parsedValue, out _))
        {
            value = parsedValue;
            consumed = index + 1;
            return true;
        }

        value = 0;
        consumed = 0;
        return false;
    }

    /// <summary>
    /// Tries to parse a int value from the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to read the value from.
    /// </param>
    /// <param name="value">
    /// The parsed value.
    /// </param>
    /// <param name="consumed">
    /// The number of bytes that have been consumed.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be parsed.
    /// </returns>
    protected static bool TryParseIdPart(
        ReadOnlySpan<byte> buffer,
        out int value,
        out int consumed)
    {
        var index = buffer.IndexOf(PartSeparator);
        var valueSpan = index == -1 ? buffer : buffer[..index];

        if (Utf8Parser.TryParse(valueSpan, out int parsedValue, out _))
        {
            value = parsedValue;
            consumed = index + 1;
            return true;
        }

        value = 0;
        consumed = 0;
        return false;
    }

    /// <summary>
    /// Tries to parse a long value from the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to read the value from.
    /// </param>
    /// <param name="value">
    /// The parsed value.
    /// </param>
    /// <param name="consumed">
    /// The number of bytes that have been consumed.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be parsed.
    /// </returns>
    protected static bool TryParseIdPart(
        ReadOnlySpan<byte> buffer,
        out long value,
        out int consumed)
    {
        var index = buffer.IndexOf(PartSeparator);
        var valueSpan = index == -1 ? buffer : buffer[..index];

        if (Utf8Parser.TryParse(valueSpan, out long parsedValue, out _))
        {
            value = parsedValue;
            consumed = index + 1;
            return true;
        }

        value = 0;
        consumed = 0;
        return false;
    }

    /// <summary>
    /// Tries to parse a boolean value from the buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to read the value from.
    /// </param>
    /// <param name="value">
    /// The parsed value.
    /// </param>
    /// <param name="consumed">
    /// The number of bytes that have been consumed.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value could be parsed.
    /// </returns>
    protected static bool TryParseIdPart(
        ReadOnlySpan<byte> buffer,
        out bool value,
        out int consumed)
    {
        var index = buffer.IndexOf(PartSeparator);
        var valueSpan = index == -1 ? buffer : buffer[..index];

        if (valueSpan.Length == 1)
        {
            value = valueSpan[0] == '1';
            consumed = index + 1;
            return true;
        }

        value = false;
        consumed = 0;
        return false;
    }

    /// <summary>
    /// Writes the given unescaped bytes with the part separator (<c>:</c>) escaped, into the given
    /// span.
    /// </summary>
    /// <param name="unescapedBytes">The unescaped bytes to write as escaped.</param>
    /// <param name="escapedBytes">The span into which the escaped bytes should be written.</param>
    /// <returns>The number of bytes written.</returns>
    private static int WriteEscapedBytes(ReadOnlySpan<byte> unescapedBytes, Span<byte> escapedBytes)
    {
        var index = 0;

        foreach (var b in unescapedBytes)
        {
            if (b == PartSeparator)
            {
                escapedBytes[index++] = Escape;
            }

            escapedBytes[index++] = b;
        }

        return index;
    }

    /// <summary>
    /// Unescapes part separators (<c>:</c>) in the given span of bytes.
    /// </summary>
    /// <param name="escapedBytes">A span with the bytes to be unescaped.</param>
    /// <returns>A span with the unescaped bytes.</returns>
    private static ReadOnlySpan<byte> Unescape(ReadOnlySpan<byte> escapedBytes)
    {
        Span<byte> unescapedBytes = new byte[escapedBytes.Length];

        var index = 0;
        var skipNext = false;

        for (var i = 0; i < escapedBytes.Length; i++)
        {
            if (skipNext)
            {
                skipNext = false;
                continue;
            }

            if (escapedBytes[i] == Escape
                && i + 1 < escapedBytes.Length
                && escapedBytes[i + 1] == PartSeparator)
            {
                unescapedBytes[index++] = PartSeparator;
                skipNext = true;
            }
            else
            {
                unescapedBytes[index++] = escapedBytes[i];
            }
        }

        return unescapedBytes[..index];
    }

    /// <summary>
    /// Finds the index of the first non-escaped part separator (<c>:</c>) in the given buffer.
    /// </summary>
    /// <param name="buffer">The buffer to search.</param>
    /// <returns>The index of the non-escaped part separator.</returns>
    private static int IndexOfPartSeparator(ReadOnlySpan<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == PartSeparator && (i == 0 || buffer[i - 1] != Escape))
            {
                return i;
            }
        }

        return -1;
    }
}
