namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;

internal static class TelemetryTextExtensions
{
    private const string HexDigits = "0123456789ABCDEF";

    extension(string value)
    {
        /// <summary>
        /// Renders line breaks as <c>\n</c>, tabs as <c>\t</c> and every other control character as a
        /// <c>\uXXXX</c> escape. Returns the original string when it contains no control characters.
        /// </summary>
        public string EscapeControlCharacters()
        {
            var first = IndexOfFirstControl(value);
            if (first < 0)
            {
                return value;
            }

            return string.Create(
                GetEscapedLength(value, first),
                (value, first),
                static (destination, state) => WriteEscaped(state.value, destination, state.first));
        }
    }

    private static int IndexOfFirstControl(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsControl(value[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static int GetEscapedLength(string value, int first)
    {
        var length = first;

        for (var i = first; i < value.Length; i++)
        {
            var c = value[i];

            if (c == '\r' && i + 1 < value.Length && value[i + 1] == '\n')
            {
                length += 2;
                i++;
            }
            else if (c is '\r' or '\n' or '\t')
            {
                length += 2;
            }
            else if (char.IsControl(c))
            {
                length += 6;
            }
            else
            {
                length++;
            }
        }

        return length;
    }

    private static void WriteEscaped(string value, Span<char> destination, int first)
    {
        value.AsSpan(0, first).CopyTo(destination);
        var position = first;

        for (var i = first; i < value.Length; i++)
        {
            var c = value[i];

            if (c == '\r' && i + 1 < value.Length && value[i + 1] == '\n')
            {
                destination[position++] = '\\';
                destination[position++] = 'n';
                i++;
            }
            else if (c is '\r' or '\n')
            {
                destination[position++] = '\\';
                destination[position++] = 'n';
            }
            else if (c == '\t')
            {
                destination[position++] = '\\';
                destination[position++] = 't';
            }
            else if (char.IsControl(c))
            {
                destination[position++] = '\\';
                destination[position++] = 'u';
                destination[position++] = HexDigits[(c >> 12) & 0xF];
                destination[position++] = HexDigits[(c >> 8) & 0xF];
                destination[position++] = HexDigits[(c >> 4) & 0xF];
                destination[position++] = HexDigits[c & 0xF];
            }
            else
            {
                destination[position++] = c;
            }
        }
    }
}
