using System.Buffers.Text;
using System.Text;

namespace HotChocolate.Text.Yaml;

/// <summary>
/// A parsed YAML document.
/// </summary>
/// <remarks>
/// The reader supports block mappings, block sequences of scalars and mappings, plain and
/// double-quoted single-line scalars, comments, and <c>[]</c> and <c>{}</c> for empty
/// collections. Every other YAML feature causes a <see cref="YamlException"/>.
/// </remarks>
public sealed partial class YamlDocument
{
    private readonly byte[] _yaml;
    private readonly DbRow[] _rows;

    private YamlDocument(byte[] yaml, DbRow[] rows)
    {
        _yaml = yaml;
        _rows = rows;
    }

    /// <summary>
    /// Gets the root value of the document, which is <see cref="YamlValueKind.Null"/> for an
    /// empty document.
    /// </summary>
    public YamlElement RootElement => new(this, 0);

    /// <summary>
    /// Parses a UTF-8 encoded YAML document.
    /// </summary>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML text.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="YamlException">
    /// The text is not valid YAML or uses a feature the reader does not support.
    /// </exception>
    public static YamlDocument Parse(ReadOnlySpan<byte> utf8Yaml)
    {
        var yaml = utf8Yaml.ToArray();
        var rows = Parser.Parse(yaml);
        return new YamlDocument(yaml, rows);
    }

    /// <summary>
    /// Parses a YAML document.
    /// </summary>
    /// <param name="yaml">The YAML text.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="YamlException">
    /// The text is not valid YAML or uses a feature the reader does not support.
    /// </exception>
    public static YamlDocument Parse(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        return Parse(Encoding.UTF8.GetBytes(yaml));
    }

    internal DbRow GetRow(int index) => _rows[index];

    internal string GetString(int index)
    {
        var row = GetRow(index);
        var text = _yaml.AsSpan(row.Location, row.Length);
        return row.IsQuoted ? Unescape(text) : Encoding.UTF8.GetString(text);
    }

    internal bool TryGetInt32(int index, out int value)
    {
        var row = GetRow(index);
        var text = _yaml.AsSpan(row.Location, row.Length);
        return Utf8Parser.TryParse(text, out value, out var consumed) && consumed == text.Length;
    }

    internal bool TryGetInt64(int index, out long value)
    {
        var row = GetRow(index);
        var text = _yaml.AsSpan(row.Location, row.Length);
        return Utf8Parser.TryParse(text, out value, out var consumed) && consumed == text.Length;
    }

    internal bool TryGetDouble(int index, out double value)
    {
        var row = GetRow(index);
        var text = _yaml.AsSpan(row.Location, row.Length);
        return Utf8Parser.TryParse(text, out value, out var consumed) && consumed == text.Length;
    }

    private static string Unescape(ReadOnlySpan<byte> text)
    {
        var builder = new StringBuilder(text.Length);
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != (byte)'\\')
            {
                continue;
            }

            builder.Append(Encoding.UTF8.GetString(text[start..i]));

            var escape = text[++i];

            switch (escape)
            {
                case (byte)'"':
                    builder.Append('"');
                    break;

                case (byte)'\\':
                    builder.Append('\\');
                    break;

                case (byte)'/':
                    builder.Append('/');
                    break;

                case (byte)'b':
                    builder.Append('\b');
                    break;

                case (byte)'f':
                    builder.Append('\f');
                    break;

                case (byte)'n':
                    builder.Append('\n');
                    break;

                case (byte)'r':
                    builder.Append('\r');
                    break;

                case (byte)'t':
                    builder.Append('\t');
                    break;

                default:
                    Utf8Parser.TryParse(text.Slice(i + 1, 4), out ushort code, out _, 'X');
                    builder.Append((char)code);
                    i += 4;
                    break;
            }

            start = i + 1;
        }

        builder.Append(Encoding.UTF8.GetString(text[start..]));
        return builder.ToString();
    }

    internal struct DbRow
    {
        public YamlValueKind Kind;
        public bool IsQuoted;
        public int Location;
        public int Length;
        public int Line;
        public int Column;
        public int NumberOfRows;
        public int ChildCount;
    }
}
