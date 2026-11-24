namespace HotChocolate.Text.Yaml;

/// <summary>
/// The exception that is thrown when a YAML document is invalid or uses a feature that the
/// reader does not support.
/// </summary>
public sealed class YamlException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="YamlException"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="line">The 1-based line of the error.</param>
    /// <param name="column">The 1-based column of the error.</param>
    public YamlException(string message, int line, int column)
        : base(message)
    {
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the 1-based line of the error.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the 1-based column of the error.
    /// </summary>
    public int Column { get; }
}
