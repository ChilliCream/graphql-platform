using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Tui;

/// <summary>
/// Measures and slices text by the terminal cells occupied by its Unicode text elements.
/// Slicing never separates the code points that form one text element.
/// </summary>
internal static class DisplayWidth
{
    private const string Ellipsis = "…";

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

    private static int GetTextElementWidth(string element) => element.GetCellWidth();
}
