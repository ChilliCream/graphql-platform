namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class Location
{
    public Location(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public int Line { get; }

    public int Column { get; }
}
