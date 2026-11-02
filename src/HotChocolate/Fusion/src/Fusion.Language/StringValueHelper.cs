using System.Text;
using static HotChocolate.Fusion.Language.CharConstants;
using static HotChocolate.Fusion.Language.Properties.FusionLanguageResources;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// Decodes the raw lexeme of a string or block string value into its semantic value.
/// The escape and block string algorithms follow the GraphQL specification. They mostly align
/// with <c>HotChocolate.Language</c>, with three deliberate exceptions: an unpaired surrogate in a
/// <c>\u</c> escape is rejected rather than passed through, whitespace-only lines do not
/// contribute to the common indentation when a block string is dedented, and standard escapes
/// (<c>\n</c>, <c>\t</c>, <c>\uXXXX</c>, etc.) are resolved inside block strings with unknown
/// escapes rejected, whereas the specification treats every backslash in a block string literally
/// except <c>\"""</c>.
/// </summary>
internal static class StringValueHelper
{
    /// <summary>
    /// Resolves the escape sequences of a regular double-quoted string value.
    /// </summary>
    public static string UnescapeString(ReadOnlySpan<char> escaped, FieldSelectionMapReader reader)
    {
        if (escaped.IndexOf(Backslash) == -1)
        {
            return escaped.ToString();
        }

        var builder = new StringBuilder(escaped.Length);
        Unescape(escaped, builder, isBlockString: false, reader);

        return builder.ToString();
    }

    /// <summary>
    /// Resolves the escape sequences and removes the common indentation of a block string value.
    /// </summary>
    public static string TrimBlockString(ReadOnlySpan<char> rawValue, FieldSelectionMapReader reader)
    {
        string value;

        if (rawValue.IndexOf(Backslash) == -1)
        {
            value = rawValue.ToString();
        }
        else
        {
            var builder = new StringBuilder(rawValue.Length);
            Unescape(rawValue, builder, isBlockString: true, reader);
            value = builder.ToString();
        }

        return DedentBlockString(value);
    }

    private static void Unescape(
        ReadOnlySpan<char> escaped,
        StringBuilder builder,
        bool isBlockString,
        FieldSelectionMapReader reader)
    {
        var position = 0;

        while (position < escaped.Length)
        {
            var code = escaped[position];

            if (code != Backslash)
            {
                builder.Append(code);
                position++;
                continue;
            }

            // skip backslash
            position++;

            if (position >= escaped.Length)
            {
                throw new FieldSelectionMapSyntaxException(
                    reader,
                    InvalidCharacterEscapeSequence,
                    Backslash);
            }

            code = escaped[position];

            if (isBlockString && code == Quote)
            {
                if (position + 2 < escaped.Length
                    && escaped[position + 1] == Quote
                    && escaped[position + 2] == Quote)
                {
                    builder.Append("\"\"\"");
                    position += 3;
                    continue;
                }

                throw new FieldSelectionMapSyntaxException(
                    reader,
                    InvalidCharacterEscapeSequence,
                    code);
            }

            if (code == 'u')
            {
                position++;
                // A \uXXXX escape encodes a single UTF-16 code unit. A code point outside the
                // basic multilingual plane is written as two consecutive \u escapes that form a
                // surrogate pair, so the pair has to be validated as a unit before it is appended.
                AppendUnicodeScalar(escaped, ref position, builder, reader);
                continue;
            }

            if (TryGetEscapeCharacter(code, out var unescaped))
            {
                builder.Append(unescaped);
                position++;
                continue;
            }

            throw new FieldSelectionMapSyntaxException(
                reader,
                InvalidCharacterEscapeSequence,
                code);
        }
    }

    private static void AppendUnicodeScalar(
        ReadOnlySpan<char> escaped,
        ref int position,
        StringBuilder builder,
        FieldSelectionMapReader reader)
    {
        var code = ReadHex4(escaped, ref position, reader);

        // A high surrogate must be immediately followed by a \uXXXX low surrogate so that the two
        // code units describe a single code point.
        if (code is >= 0xD800 and <= 0xDBFF)
        {
            if (position + 1 < escaped.Length
                && escaped[position] == Backslash
                && escaped[position + 1] == 'u')
            {
                position += 2;
                var low = ReadHex4(escaped, ref position, reader);

                if (low is < 0xDC00 or > 0xDFFF)
                {
                    throw new FieldSelectionMapSyntaxException(reader, InvalidUnicodeEscapeSequence);
                }

                builder.Append((char)code);
                builder.Append((char)low);
                return;
            }

            // Unlike HotChocolate, which silently drops a trailing high surrogate, we reject it.
            // Throwing here is stricter and matches the GraphQL specification draft.
            throw new FieldSelectionMapSyntaxException(reader, InvalidUnicodeEscapeSequence);
        }

        // A low surrogate without a preceding high surrogate is not a valid code point.
        if (code is >= 0xDC00 and <= 0xDFFF)
        {
            throw new FieldSelectionMapSyntaxException(reader, InvalidUnicodeEscapeSequence);
        }

        builder.Append((char)code);
    }

    private static int ReadHex4(
        ReadOnlySpan<char> escaped,
        ref int position,
        FieldSelectionMapReader reader)
    {
        if (position + 4 > escaped.Length)
        {
            throw new FieldSelectionMapSyntaxException(reader, InvalidUnicodeEscapeSequence);
        }

        var slice = escaped.Slice(position, 4);

        // A \uXXXX escape requires exactly four hexadecimal digits. NumberStyles.HexNumber would
        // also accept surrounding whitespace, so each digit is checked explicitly to reject any
        // non-hex character.
        var value = 0;

        for (var i = 0; i < 4; i++)
        {
            var c = slice[i];

            if (!char.IsAsciiHexDigit(c))
            {
                throw new FieldSelectionMapSyntaxException(reader, InvalidUnicodeEscapeSequence);
            }

            value = (value << 4) | HexValue(c);
        }

        position += 4;

        return value;
    }

    private static int HexValue(char c)
        => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            _ => c - 'A' + 10
        };

    private static bool TryGetEscapeCharacter(char code, out char unescaped)
    {
        switch (code)
        {
            case Quote:
                unescaped = Quote;
                return true;
            case '/':
                unescaped = '/';
                return true;
            case Backslash:
                unescaped = Backslash;
                return true;
            case 'b':
                unescaped = '\b';
                return true;
            case 'f':
                unescaped = '\f';
                return true;
            case 'n':
                unescaped = LineFeed;
                return true;
            case 'r':
                unescaped = Return;
                return true;
            case 't':
                unescaped = HorizontalTab;
                return true;
            default:
                unescaped = default;
                return false;
        }
    }

    // Removes the common indentation and the blank edge lines of a block string as described by
    // the GraphQL specification. Whitespace-only lines are excluded from the common indentation
    // and whitespace-only edge lines are trimmed. This intentionally diverges from
    // HotChocolate's current behavior, which counts whitespace-only lines toward the common
    // indentation and only trims fully empty edge lines.
    private static string DedentBlockString(string value)
    {
        var lines = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        // Determine the common indentation of all lines except the first.
        int? commonIndent = null;
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var indent = LeadingWhitespace(line);

            if (indent < line.Length && (commonIndent is null || indent < commonIndent))
            {
                commonIndent = indent;
            }
        }

        // Remove the common indentation from all lines except the first.
        if (commonIndent is > 0)
        {
            for (var i = 1; i < lines.Length; i++)
            {
                lines[i] = lines[i].Length >= commonIndent
                    ? lines[i][commonIndent.Value..]
                    : lines[i];
            }
        }

        var start = 0;
        var end = lines.Length;

        // Remove leading blank lines.
        while (start < end && IsBlank(lines[start]))
        {
            start++;
        }

        // Remove trailing blank lines.
        while (end > start && IsBlank(lines[end - 1]))
        {
            end--;
        }

        return string.Join("\n", lines, start, end - start);
    }

    private static int LeadingWhitespace(string line)
    {
        var i = 0;
        while (i < line.Length && (line[i] == Space || line[i] == HorizontalTab))
        {
            i++;
        }

        return i;
    }

    private static bool IsBlank(string line) => LeadingWhitespace(line) == line.Length;
}
