using System.Text;

namespace HotChocolate.Text.Yaml;

/// <summary>
/// Provides read-only access to YAML block mappings, block sequences, and single-line scalars.
/// </summary>
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
        var parsed = YamlCoreSchema.TryParseInt64(GetString(index), out var integer);
        value = (int)integer;
        return parsed && integer is >= int.MinValue and <= int.MaxValue;
    }

    internal bool TryGetInt64(int index, out long value)
        => YamlCoreSchema.TryParseInt64(GetString(index), out value);

    internal bool TryGetDouble(int index, out double value)
        => YamlCoreSchema.TryParseDouble(GetString(index), out value);

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
                case (byte)'x':
                    builder.Append((char)ParseHex(text.Slice(i + 1, 2)));
                    i += 2;
                    break;

                case (byte)'u':
                    builder.Append((char)ParseHex(text.Slice(i + 1, 4)));
                    i += 4;
                    break;

                case (byte)'U':
                    builder.Append(char.ConvertFromUtf32(ParseHex(text.Slice(i + 1, 8))));
                    i += 8;
                    break;

                default:
                    TryGetEscapedCharacter(escape, out var character);
                    builder.Append(character);
                    break;
            }

            start = i + 1;
        }

        builder.Append(Encoding.UTF8.GetString(text[start..]));
        return builder.ToString();
    }

    internal static int ParseHex(ReadOnlySpan<byte> digits)
    {
        var value = 0;

        foreach (var digit in digits)
        {
            value = (value << 4) | HexDigitValue(digit);
        }

        return value;
    }

    internal static int HexDigitValue(byte digit)
        => digit switch
        {
            >= (byte)'0' and <= (byte)'9' => digit - '0',
            >= (byte)'a' and <= (byte)'f' => digit - 'a' + 10,
            >= (byte)'A' and <= (byte)'F' => digit - 'A' + 10,
            _ => -1
        };

    internal static bool TryGetEscapedCharacter(byte escape, out char character)
    {
        character = escape switch
        {
            (byte)'0' => '\0',
            (byte)'a' => '\a',
            (byte)'b' => '\b',
            (byte)'t' or (byte)'\t' => '\t',
            (byte)'n' => '\n',
            (byte)'v' => '\v',
            (byte)'f' => '\f',
            (byte)'r' => '\r',
            (byte)'e' => '\u001B',
            (byte)' ' => ' ',
            (byte)'"' => '"',
            (byte)'/' => '/',
            (byte)'\\' => '\\',
            (byte)'N' => '\u0085',
            (byte)'_' => '\u00A0',
            (byte)'L' => '\u2028',
            (byte)'P' => '\u2029',
            _ => '\uFFFF'
        };

        return character != '\uFFFF';
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
