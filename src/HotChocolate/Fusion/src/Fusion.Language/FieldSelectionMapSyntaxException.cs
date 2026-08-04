namespace HotChocolate.Fusion.Language;

[Serializable]
public sealed class FieldSelectionMapSyntaxException : Exception
{
    internal FieldSelectionMapSyntaxException(
        FieldSelectionMapReader reader,
        string message)
        : base(message)
    {
        Position = reader.Position;
        Line = reader.Line;
        Column = reader.Column;
    }

    internal FieldSelectionMapSyntaxException(
        FieldSelectionMapReader reader,
        string message,
        params object[] args)
        : this(reader, string.Format(message, args))
    {
    }

    public int Position { get; }

    public int Line { get; }

    public int Column { get; }
}
