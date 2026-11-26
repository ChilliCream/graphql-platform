using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace HotChocolate.Text.Yaml;

/// <summary>
/// Writes UTF-8 encoded YAML to a buffer.
/// </summary>
public sealed class YamlWriter
{
    private const int MaxIndentation = 64;
    private static ReadOnlySpan<byte> Spaces => "                                                                "u8;

    private static readonly SearchValues<char> s_indicators =
        SearchValues.Create(",[]{}#&*!|>'\"%@`");

    private static readonly SearchValues<char> s_flowIndicators =
        SearchValues.Create(",[]{}");

    private IBufferWriter<byte> _writer;
    private Frame[] _frames = new Frame[16];
    private int _depth;
    private bool _afterPropertyName;
    private bool _lineOpen;
    private bool _continueLine;

    /// <summary>
    /// Creates a writer for the specified buffer.
    /// </summary>
    public YamlWriter(IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        _writer = writer;
    }

    /// <summary>
    /// Resets the writer to start a new document in the specified buffer.
    /// </summary>
    public void Reset(IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        _writer = writer;
        _depth = 0;
        _afterPropertyName = false;
        _lineOpen = false;
        _continueLine = false;
    }

    /// <summary>
    /// Writes the start of a mapping.
    /// </summary>
    public void WriteStartObject() => WriteStartContainer(ContainerKind.Object);

    /// <summary>
    /// Writes the end of a mapping.
    /// </summary>
    public void WriteEndObject()
    {
        Debug.Assert(_depth > 0 && _frames[_depth - 1].Kind == ContainerKind.Object);
        WriteEndContainer("{}"u8);
    }

    /// <summary>
    /// Writes the start of a sequence.
    /// </summary>
    public void WriteStartArray() => WriteStartContainer(ContainerKind.Array);

    /// <summary>
    /// Starts a single-line sequence of scalar values.
    /// </summary>
    public void WriteStartFlowArray()
    {
        WriteValuePrefix();
        WriteBytes("["u8);
        Push(ContainerKind.FlowArray, Placement.Root, 0);
    }

    /// <summary>
    /// Writes the end of a sequence.
    /// </summary>
    public void WriteEndArray()
    {
        Debug.Assert(_depth > 0 && _frames[_depth - 1].Kind != ContainerKind.Object);

        if (_frames[_depth - 1].Kind == ContainerKind.FlowArray)
        {
            _depth--;
            WriteBytes("]"u8);
            CompleteValue();
            return;
        }

        WriteEndContainer("[]"u8);
    }

    /// <summary>
    /// Writes the key of a mapping entry.
    /// </summary>
    public void WritePropertyName(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        Debug.Assert(_depth > 0 && _frames[_depth - 1].Kind == ContainerKind.Object);
        Debug.Assert(!_afterPropertyName);

        ref var frame = ref _frames[_depth - 1];
        StartLine(frame.Indent);
        WriteScalar(propertyName);
        WriteBytes(":"u8);
        frame.Count++;
        _afterPropertyName = true;
    }

    /// <summary>
    /// Writes a string value, or YAML null if <paramref name="value"/> is null.
    /// </summary>
    public void WriteStringValue(string? value)
    {
        if (value is null)
        {
            WriteNullValue();
            return;
        }

        WriteValuePrefix();
        WriteScalar(value);
        CompleteValue();
    }

    /// <summary>
    /// Writes a string in literal block style when possible.
    /// </summary>
    public void WriteLiteralStringValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!CanWriteBlock(value))
        {
            WriteStringValue(value);
            return;
        }

        WriteBlock("|"u8, value);
    }

    /// <summary>
    /// Writes a string in folded block style when folding preserves its value.
    /// </summary>
    public void WriteFoldedStringValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!CanWriteBlock(value) || !CanFold(value))
        {
            WriteLiteralStringValue(value);
            return;
        }

        WriteBlock(">"u8, value);
    }

    /// <summary>
    /// Writes a number.
    /// </summary>
    public void WriteNumberValue(int value)
        => WriteFormattedNumber(value);

    /// <summary>
    /// Writes a number.
    /// </summary>
    public void WriteNumberValue(long value)
        => WriteFormattedNumber(value);

    /// <summary>
    /// Writes a number.
    /// </summary>
    public void WriteNumberValue(ulong value)
        => WriteFormattedNumber(value);

    /// <summary>
    /// Writes a number.
    /// </summary>
    public void WriteNumberValue(double value)
    {
        if (double.IsFinite(value))
        {
            WriteFormattedNumber(value);
            return;
        }

        WriteValuePrefix();
        WriteBytes(double.IsNaN(value) ? ".nan"u8 : value > 0 ? ".inf"u8 : "-.inf"u8);
        CompleteValue();
    }

    /// <summary>
    /// Writes a number.
    /// </summary>
    public void WriteNumberValue(decimal value)
        => WriteFormattedNumber(value);

    /// <summary>
    /// Writes a preformatted UTF-8 JSON number verbatim.
    /// </summary>
    public void WriteNumberValue(ReadOnlySpan<byte> utf8FormattedNumber)
    {
        WriteValuePrefix();
        WriteBytes(utf8FormattedNumber);
        CompleteValue();
    }

    /// <summary>
    /// Writes a boolean.
    /// </summary>
    public void WriteBooleanValue(bool value)
    {
        WriteValuePrefix();
        WriteBytes(value ? "true"u8 : "false"u8);
        CompleteValue();
    }

    /// <summary>
    /// Writes <c>null</c>.
    /// </summary>
    public void WriteNullValue()
    {
        WriteValuePrefix();
        WriteBytes("null"u8);
        CompleteValue();
    }

    private void WriteStartContainer(ContainerKind kind)
    {
        if (_depth == 0)
        {
            Push(kind, Placement.Root, 0);
            return;
        }

        ref var parent = ref _frames[_depth - 1];
        Debug.Assert(parent.Kind != ContainerKind.FlowArray);

        if (parent.Kind == ContainerKind.Object)
        {
            Debug.Assert(_afterPropertyName);
            _afterPropertyName = false;
            Push(kind, Placement.PropertyValue, parent.Indent + 2);
            return;
        }

        StartLine(parent.Indent);
        parent.Count++;
        var indent = parent.Indent + 2;

        if (kind == ContainerKind.Object)
        {
            WriteBytes("- "u8);
            _continueLine = true;
        }
        else
        {
            WriteBytes("-"u8);
        }

        Push(kind, Placement.ArrayItem, indent);
    }

    private void WriteEndContainer(ReadOnlySpan<byte> empty)
    {
        var frame = _frames[--_depth];

        if (frame.Count == 0)
        {
            if (frame.Placement == Placement.PropertyValue
                || (frame.Placement == Placement.ArrayItem && frame.Kind == ContainerKind.Array))
            {
                WriteBytes(" "u8);
            }

            WriteBytes(empty);
            _continueLine = false;
        }

        CompleteValue();
    }

    private void WriteValuePrefix()
    {
        if (_depth == 0)
        {
            return;
        }

        ref var frame = ref _frames[_depth - 1];

        switch (frame.Kind)
        {
            case ContainerKind.Object:
                Debug.Assert(_afterPropertyName);
                _afterPropertyName = false;
                WriteBytes(" "u8);
                break;

            case ContainerKind.Array:
                StartLine(frame.Indent);
                WriteBytes("- "u8);
                frame.Count++;
                break;

            default:
                if (frame.Count > 0)
                {
                    WriteBytes(", "u8);
                }

                frame.Count++;
                break;
        }
    }

    private void CompleteValue()
    {
        _lineOpen = true;

        if (_depth == 0)
        {
            WriteBytes("\n"u8);
            _lineOpen = false;
        }
    }

    private void StartLine(int indent)
    {
        if (_continueLine)
        {
            _continueLine = false;
            return;
        }

        if (_lineOpen)
        {
            WriteBytes("\n"u8);
        }

        WriteIndentation(indent);
        _lineOpen = true;
    }

    private void WriteBlock(ReadOnlySpan<byte> indicator, string value)
    {
        WriteValuePrefix();
        WriteBytes(indicator);

        var trailingLineBreaks = value.Length - value.TrimEnd('\n').Length;
        var content = value.AsSpan();

        switch (trailingLineBreaks)
        {
            case 0:
                WriteBytes("-"u8);
                break;

            case 1:
                content = content[..^1];
                break;

            default:
                WriteBytes("+"u8);
                content = content[..^1];
                break;
        }

        var indent = _depth == 0 ? 2 : _frames[_depth - 1].Indent + 2;

        foreach (var line in content.EnumerateLines())
        {
            WriteBytes("\n"u8);

            if (!line.IsEmpty)
            {
                WriteIndentation(indent);
                WriteText(line);
            }
        }

        CompleteValue();
    }

    private void WriteScalar(string value)
    {
        var inFlowArray = _depth > 0 && _frames[_depth - 1].Kind == ContainerKind.FlowArray;

        if (IsPlain(value, inFlowArray))
        {
            WriteText(value);
            return;
        }

        WriteBytes("\""u8);
        var start = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            if (current is not ('"' or '\\') && !char.IsControl(current))
            {
                continue;
            }

            WriteText(value.AsSpan(start, i - start));
            start = i + 1;

            switch (current)
            {
                case '"':
                    WriteBytes("\\\""u8);
                    break;

                case '\\':
                    WriteBytes("\\\\"u8);
                    break;

                case '\n':
                    WriteBytes("\\n"u8);
                    break;

                case '\r':
                    WriteBytes("\\r"u8);
                    break;

                case '\t':
                    WriteBytes("\\t"u8);
                    break;

                case '\b':
                    WriteBytes("\\b"u8);
                    break;

                case '\f':
                    WriteBytes("\\f"u8);
                    break;

                default:
                    WriteText($"\\u{(int)current:X4}");
                    break;
            }
        }

        WriteText(value.AsSpan(start));
        WriteBytes("\""u8);
    }

    private void WriteFormattedNumber<T>(T value) where T : IUtf8SpanFormattable
    {
        WriteValuePrefix();

        var span = _writer.GetSpan(64);
        var formatted = value.TryFormat(span, out var written, default, CultureInfo.InvariantCulture);
        Debug.Assert(formatted);
        _writer.Advance(written);

        CompleteValue();
    }

    private void WriteIndentation(int indent)
    {
        while (indent > MaxIndentation)
        {
            WriteBytes(Spaces);
            indent -= MaxIndentation;
        }

        WriteBytes(Spaces[..indent]);
    }

    private void WriteText(ReadOnlySpan<char> text)
    {
        var span = _writer.GetSpan(Encoding.UTF8.GetMaxByteCount(text.Length));
        var written = Encoding.UTF8.GetBytes(text, span);
        _writer.Advance(written);
    }

    private void WriteBytes(ReadOnlySpan<byte> bytes) => _writer.Write(bytes);

    private void Push(ContainerKind kind, Placement placement, int indent)
    {
        if (_depth == _frames.Length)
        {
            Array.Resize(ref _frames, _frames.Length * 2);
        }

        _frames[_depth++] = new Frame(kind, placement, indent);
    }

    private static bool IsPlain(string value, bool inFlowArray)
    {
        if (value.Length == 0 || value[0] is ' ' || value[^1] is ' ' or ':')
        {
            return false;
        }

        var first = value[0];

        if (s_indicators.Contains(first)
            || (first is '-' or '?' or ':' && (value.Length == 1 || value[1] == ' ')))
        {
            return false;
        }

        if (inFlowArray && value.AsSpan().ContainsAny(s_flowIndicators))
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            if (char.IsControl(current)
                || current is '\u2028' or '\u2029' or '\uFEFF'
                || (current == ':' && value[i + 1] == ' ')
                || (current == '#' && value[i - 1] == ' '))
            {
                return false;
            }
        }

        return YamlCoreSchema.Resolve(value) == YamlValueKind.String;
    }

    private static bool CanWriteBlock(string value)
    {
        foreach (var current in value)
        {
            if ((current is not ('\n' or '\t') && char.IsControl(current)) || current is '\u2028' or '\u2029')
            {
                return false;
            }
        }

        foreach (var line in value.AsSpan().EnumerateLines())
        {
            if (!line.IsEmpty)
            {
                return line[0] != ' ';
            }
        }

        return true;
    }

    private static bool CanFold(string value)
    {
        var previousIsIndented = true;

        foreach (var line in value.AsSpan().TrimEnd('\n').EnumerateLines())
        {
            var isIndented = !line.IsEmpty && line[0] == ' ';

            if (!previousIsIndented && !isIndented)
            {
                return false;
            }

            previousIsIndented = isIndented;
        }

        return true;
    }

    private enum ContainerKind : byte
    {
        Object,
        Array,
        FlowArray
    }

    private enum Placement : byte
    {
        Root,
        PropertyValue,
        ArrayItem
    }

    private struct Frame(ContainerKind kind, Placement placement, int indent)
    {
        public readonly ContainerKind Kind = kind;
        public readonly Placement Placement = placement;
        public readonly int Indent = indent;
        public int Count;
    }
}
