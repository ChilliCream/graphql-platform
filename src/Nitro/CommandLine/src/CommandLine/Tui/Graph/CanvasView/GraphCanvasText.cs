using System.Buffers;
using System.Text;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.CanvasView;

/// <summary>
/// Encodes graph canvas text into characters that each occupy one terminal cell.
/// </summary>
internal static class GraphCanvasText
{
    public static string Encode(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var remaining = value.AsSpan();

        foreach (var rune in value.EnumerateRunes())
        {
            var status = Rune.DecodeFromUtf16(remaining, out _, out var consumed);
            if (status != OperationStatus.Done)
            {
                builder.Append('?');
                remaining = remaining[1..];
                continue;
            }

            builder.Append(IsSingleCell(rune) ? (char)rune.Value : '?');
            remaining = remaining[consumed..];
        }

        return builder.ToString();
    }

    public static string Truncate(string? value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var encoded = Encode(value);
        if (encoded.Length <= width)
        {
            return encoded;
        }

        if (width == 1)
        {
            return "…";
        }

        var builder = new StringBuilder(width);
        var count = 0;
        foreach (var character in encoded)
        {
            if (count == width - 1)
            {
                break;
            }

            builder.Append(character);
            count++;
        }

        builder.Append('…');
        return builder.ToString();
    }

    public static string PadRight(string? value, int width)
    {
        var encoded = Truncate(value, width);
        return encoded.Length < width ? encoded.PadRight(width) : encoded;
    }

    private static bool IsSingleCell(Rune rune)
        => rune.IsBmp && ((char)rune.Value).GetCellWidth() == 1;
}
