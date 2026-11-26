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
    /// <param name="reason">
    /// What is wrong with the document, without the position and without a closing period.
    /// </param>
    /// <param name="line">The 1-based line of the error.</param>
    /// <param name="column">The 1-based column of the error.</param>
    public YamlException(string reason, int line, int column)
        : base($"{reason} (line {line}, column {column}).")
    {
        Reason = reason;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets what is wrong with the document, without the position and without a closing period.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the 1-based line of the error.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the 1-based column of the error.
    /// </summary>
    public int Column { get; }
}
