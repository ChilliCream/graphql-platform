namespace HotChocolate.Language.Utilities;

/// <summary>
/// This interface represents a GraphQL syntax writer.
/// </summary>
public interface ISyntaxWriter
{
    /// <summary>
    /// Increase writer indentation.
    /// </summary>
    void Indent();

    /// <summary>
    /// Decrease writer indentation.
    /// </summary>
    void Unindent();

    /// <summary>
    /// Write a single character.
    /// </summary>
    /// <param name="c">
    /// The characted that shall be written.
    /// </param>
    void Write(char c);

    /// <summary>
    /// Write a string.
    /// </summary>
    /// <param name="s">
    /// The string that shall be written.
    /// </param>
    void Write(string s);

    /// <summary>
    /// Write a line if the <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    /// <param name="condition">
    /// The condition that defines if a line shall be written.
    /// </param>
    void WriteLine(bool condition = true);

    /// <summary>
    /// Write a space if the <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    /// <param name="condition">
    /// The condition that defines if a space shall be written.
    /// </param>
    void WriteSpace(bool condition = true);

    /// <summary>
    /// Write the current indentation if the <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    /// <param name="condition">
    /// The condition that defines if the current indentation shall be written.
    /// </param>
    void WriteIndent(bool condition = true);
}
