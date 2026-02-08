using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text.Json;

namespace HotChocolate.Text.Json;

public sealed partial class JsonWriter
{
    /// <summary>
    /// Writes the value (as a JSON number) as an element of a JSON array.
    /// </summary>
    /// <param name="utf8FormattedNumber">The value to write.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="utf8FormattedNumber"/> does not represent a valid JSON number.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    /// <remarks>
    /// Writes the <see cref="int"/> using the default <see cref="StandardFormat"/> (that is, 'G'), for example: 32767.
    /// </remarks>
    public void WriteNumberValue(ReadOnlySpan<byte> utf8FormattedNumber)
    {
        ValidateValue(utf8FormattedNumber);

        if (_indented)
        {
            WriteNumberValueIndented(utf8FormattedNumber);
        }
        else
        {
            WriteNumberValueMinimized(utf8FormattedNumber);
        }

        SetFlagToAddListSeparatorBeforeNextItem();
        _tokenType = JsonTokenType.Number;
    }

    public void WriteNumberValue(int value)
    {
        Span<byte> buffer = stackalloc byte[11]; // -2147483648
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        WriteNumberValue(buffer[..bytesWritten]);
    }

    public void WriteNumberValue(uint value)
    {
        Span<byte> buffer = stackalloc byte[10]; // 4294967295
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        WriteNumberValue(buffer[..bytesWritten]);
    }

    public void WriteNumberValue(long value)
    {
        Span<byte> buffer = stackalloc byte[20]; // -9223372036854775808
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        WriteNumberValue(buffer[..bytesWritten]);
    }

    public void WriteNumberValue(ulong value)
    {
        Span<byte> buffer = stackalloc byte[20]; // 18446744073709551615
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        WriteNumberValue(buffer[..bytesWritten]);
    }

    public void WriteNumberValue(short value)
    {
        Span<byte> buffer = stackalloc byte[6]; // -32768
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        WriteNumberValue(buffer[..bytesWritten]);
    }

    public void WriteNumberValue(ushort value)
    {
        Span<byte> buffer = stackalloc byte[5]; // 65535
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        WriteNumberValue(buffer[..bytesWritten]);
    }

    public void WriteNumberValue(byte value)
    {
        Span<byte> buffer = stackalloc byte[3]; // 255
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        WriteNumberValue(buffer[..bytesWritten]);
    }

    public void WriteNumberValue(float value)
    {
        Span<byte> buffer = stackalloc byte[32];
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        WriteNumberValue(buffer[..bytesWritten]);
    }

    public void WriteNumberValue(double value)
    {
        Span<byte> buffer = stackalloc byte[32];
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        WriteNumberValue(buffer[..bytesWritten]);
    }

    public void WriteNumberValue(decimal value)
    {
        Span<byte> buffer = stackalloc byte[31]; // max decimal length
        Utf8Formatter.TryFormat(value, buffer, out var bytesWritten);
        WriteNumberValue(buffer[..bytesWritten]);
    }

    private void WriteNumberValueMinimized(ReadOnlySpan<byte> utf8Value)
    {
        var maxRequired = utf8Value.Length + 1; // Optionally, 1 list separator
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        utf8Value.CopyTo(output[bytesWritten..]);
        bytesWritten += utf8Value.Length;

        _writer.Advance(bytesWritten);
    }

    private void WriteNumberValueIndented(ReadOnlySpan<byte> utf8Value)
    {
        var indent = Indentation;
        Debug.Assert(indent <= _indentLength * _maxDepth);
        Debug.Assert(utf8Value.Length < int.MaxValue - indent - 1 - _newLineLength);

        var maxRequired = indent + utf8Value.Length + 1 + _newLineLength; // Optionally, 1 list separator and 1-2 bytes for new line
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        if (_tokenType != JsonTokenType.PropertyName)
        {
            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output, ref bytesWritten);
            }

            WriteIndentation(output[bytesWritten..], indent);
            bytesWritten += indent;
        }

        utf8Value.CopyTo(output[bytesWritten..]);
        bytesWritten += utf8Value.Length;

        _writer.Advance(bytesWritten);
    }
}
