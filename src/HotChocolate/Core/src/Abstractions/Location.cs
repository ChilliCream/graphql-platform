using HotChocolate.Properties;

namespace HotChocolate;

public readonly struct Location : IComparable<Location>
{
    public Location(int line, int column)
    {
        if (line < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(line),
                line,
                AbstractionResources.Location_Line_Is_1_Based);
        }

        if (column < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(column),
                column,
                AbstractionResources.Location_Column_Is_1_Based);
        }

        Line = line;
        Column = column;
    }

    public int Line { get; }

    public int Column { get; }

    public int CompareTo(Location other)
    {
        var lineComparison = Line.CompareTo(other.Line);

        if (lineComparison != 0)
        {
            return lineComparison;
        }

        return Column.CompareTo(other.Column);
    }
}
