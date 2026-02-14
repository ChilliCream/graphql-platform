using System.Diagnostics;
using System.Text.Json;

namespace HotChocolate.Text.Json;

public sealed partial class JsonWriter
{
    /// <summary>
    /// Writes the JSON literal "null" as an element of a JSON array.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    public void WriteNullValue()
    {
        if (HasDeferredPropertyName)
        {
            DiscardDeferredPropertyName();
            return;
        }

        if (IgnoreNullListElements && IsInArray)
        {
            return;
        }

        WriteLiteralByOptions(JsonConstants.NullValue);

        SetFlagToAddListSeparatorBeforeNextItem();
        _tokenType = JsonTokenType.Null;
    }

    /// <summary>
    /// Writes the <see cref="bool"/> value (as a JSON literal "true" or "false") as an element of a JSON array.
    /// </summary>
    /// <param name="value">The value write.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    public void WriteBooleanValue(bool value)
    {
        FlushDeferredPropertyName();

        if (value)
        {
            WriteLiteralByOptions(JsonConstants.TrueValue);
            _tokenType = JsonTokenType.True;
        }
        else
        {
            WriteLiteralByOptions(JsonConstants.FalseValue);
            _tokenType = JsonTokenType.False;
        }

        SetFlagToAddListSeparatorBeforeNextItem();
    }

    private void WriteLiteralByOptions(ReadOnlySpan<byte> utf8Value)
    {
        if (_indented)
        {
            WriteLiteralIndented(utf8Value);
        }
        else
        {
            WriteLiteralMinimized(utf8Value);
        }
    }

    private void WriteLiteralMinimized(ReadOnlySpan<byte> utf8Value)
    {
        Debug.Assert(utf8Value.Length <= 5);

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

    private void WriteLiteralIndented(ReadOnlySpan<byte> utf8Value)
    {
        var indent = Indentation;
        Debug.Assert(indent <= _indentLength * _maxDepth);
        Debug.Assert(utf8Value.Length <= 5);

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
