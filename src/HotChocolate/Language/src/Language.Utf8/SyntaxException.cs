namespace HotChocolate.Language;

/// <summary>
/// Represents a GraphQL syntax error.
/// </summary>
[Serializable]
public class SyntaxException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="SyntaxException"/>.
    /// </summary>
    /// <param name="reader">
    /// The reader that encountered the syntax error.
    /// </param>
    /// <param name="message">
    /// The message that describes the error.
    /// </param>
    internal SyntaxException(Utf8GraphQLReader reader, string message) : base(message)
    {
        Position = reader.Position;
        Line = reader.Line;
        Column = reader.Column;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SyntaxException"/>.
    /// </summary>
    /// <param name="reader">
    /// The reader that encountered the syntax error.
    /// </param>
    /// <param name="message">
    /// The message that describes the error.
    /// </param>
    /// <param name="args">
    /// The arguments to format the message.
    /// </param>
    internal SyntaxException(Utf8GraphQLReader reader, string message, params object[] args)
        : this(reader, string.Format(message, args))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SyntaxException"/>.
    /// </summary>
    /// <param name="message">
    /// The message that describes the error.
    /// </param>
    /// <param name="position">
    /// The position of the syntax error.
    /// </param>
    /// <param name="line">
    /// The line of the syntax error.
    /// </param>
    /// <param name="column">
    /// The column of the syntax error.
    /// </param>
    internal SyntaxException(string message, int position, int line, int column) : base(message)
    {
        Position = position;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the position of the syntax error.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Gets the line of the syntax error.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column of the syntax error.
    /// </summary>
    public int Column { get; }
}
