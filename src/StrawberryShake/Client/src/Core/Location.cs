using StrawberryShake.Properties;

namespace StrawberryShake;

public readonly struct Location
{
    public Location(int line, int column)
    {
        if (line < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(line), line,
                Resources.Location_Location_Line_OutOfRange);
        }

        if (column < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(column), column,
                Resources.Location_Location_Column_OutOfRange);
        }

        Line = line;
        Column = column;
    }

    public int Line { get; }

    public int Column { get; }
}
