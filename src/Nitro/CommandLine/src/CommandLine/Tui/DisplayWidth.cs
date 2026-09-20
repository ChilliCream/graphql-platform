using System.Globalization;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Tui;

/// <summary>
/// Measures and slices text by the terminal cells occupied by its Unicode text elements.
/// Slicing never separates the code points that form one text element.
/// </summary>
internal static class DisplayWidth
{
    private const string Ellipsis = "…";

    /// <summary>
    /// The lowest code point of every contiguous run of terminal-wide code points this class measures
    /// at two cells. Paired with <see cref="s_wideRangeEnds"/> at the same index.
    /// </summary>
    private static readonly int[] s_wideRangeStarts =
    [
        0x1100, 0x2E80, 0x3041, 0x3400, 0x4E00, 0xA000, 0xAC00, 0xF900, 0xFE30,
        0xFF00, 0xFFE0, 0x1F1E6, 0x1F200, 0x1F300, 0x1F600, 0x1F680, 0x1F900,
        0x1FA70, 0x20000, 0x30000
    ];

    private static readonly int[] s_wideRangeEnds =
    [
        0x115F, 0x303E, 0x33FF, 0x4DBF, 0x9FFF, 0xA4CF, 0xD7A3, 0xFAFF, 0xFE4F,
        0xFF60, 0xFFE6, 0x1F1FF, 0x1F2FF, 0x1F5FF, 0x1F64F, 0x1F6FF, 0x1F9FF,
        0x1FAFF, 0x2FFFD, 0x3FFFD
    ];

    /// <summary>
    /// Returns the number of terminal cells occupied by <paramref name="value"/>.
    /// </summary>
    public static int Measure(string value)
    {
        var width = 0;
        var elements = StringInfo.GetTextElementEnumerator(value);

        while (elements.MoveNext())
        {
            width += GetTextElementWidth((string)elements.Current);
        }

        return width;
    }

    /// <summary>
    /// Returns the first Unicode text element in <paramref name="value"/>, or an empty string when
    /// <paramref name="value"/> is empty.
    /// </summary>
    public static string FirstTextElement(string value)
    {
        var elements = StringInfo.GetTextElementEnumerator(value);
        return elements.MoveNext() ? (string)elements.Current : string.Empty;
    }

    /// <summary>
    /// Returns the longest prefix of <paramref name="value"/> that fits within <paramref name="width"/>
    /// terminal cells. A non-positive width returns an empty string.
    /// </summary>
    public static string Slice(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var used = 0;
        var end = 0;
        var elements = StringInfo.GetTextElementEnumerator(value);

        while (elements.MoveNext())
        {
            var element = (string)elements.Current;
            var elementWidth = GetTextElementWidth(element);

            if (used + elementWidth > width)
            {
                break;
            }

            used += elementWidth;
            end += element.Length;
        }

        return value[..end];
    }

    /// <summary>
    /// Truncates <paramref name="value"/> to <paramref name="width"/> terminal cells and appends an
    /// ellipsis when text is omitted. A non-positive width returns an empty string.
    /// </summary>
    public static string Truncate(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (Measure(value) <= width)
        {
            return value;
        }

        if (width == 1)
        {
            return Ellipsis;
        }

        return Slice(value, width - Measure(Ellipsis)) + Ellipsis;
    }

    /// <summary>
    /// Truncates <paramref name="value"/> to <paramref name="width"/> terminal cells, then appends
    /// spaces until it occupies that width. A non-positive width returns an empty string.
    /// </summary>
    public static string PadRight(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var truncated = Truncate(value, width);
        var truncatedWidth = Measure(truncated);
        return truncatedWidth >= width ? truncated : truncated + new string(' ', width - truncatedWidth);
    }

    /// <summary>
    /// Truncates <paramref name="value"/> to <paramref name="width"/> terminal cells, then prepends
    /// spaces until it occupies that width. A non-positive width returns an empty string.
    /// </summary>
    public static string PadLeft(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var truncated = Truncate(value, width);
        var truncatedWidth = Measure(truncated);
        return truncatedWidth >= width ? truncated : new string(' ', width - truncatedWidth) + truncated;
    }

    private static int GetTextElementWidth(string element)
    {
        var width = 0;

        foreach (var rune in element.EnumerateRunes())
        {
            width = Math.Max(width, GetRuneWidth(rune));
        }

        return width;
    }

    private static int GetRuneWidth(Rune rune)
    {
        if (rune.Value == 0x200D
            || Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.EnclosingMark)
        {
            return 0;
        }

        var value = rune.Value;

        if (value < 0x1100)
        {
            return 1;
        }

        for (var i = 0; i < s_wideRangeStarts.Length; i++)
        {
            if (value < s_wideRangeStarts[i])
            {
                return 1;
            }

            if (value <= s_wideRangeEnds[i])
            {
                return 2;
            }
        }

        return 1;
    }
}
