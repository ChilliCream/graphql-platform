namespace Mocha.Analyzers.FileBuilders;

/// <summary>
/// Defines the contract for a file builder that generates C# source code
/// using a structured sequence of write operations.
/// </summary>
public interface IFileBuilder : IDisposable
{
    /// <summary>
    /// Writes the standard auto-generated file header.
    /// </summary>
    void WriteHeader();

    /// <summary>
    /// Writes the opening namespace declaration.
    /// </summary>
    void WriteBeginNamespace();

    /// <summary>
    /// Writes the closing namespace brace.
    /// </summary>
    void WriteEndNamespace();

    /// <summary>
    /// Writes the opening class declaration.
    /// </summary>
    void WriteBeginClass();

    /// <summary>
    /// Writes the closing class brace.
    /// </summary>
    void WriteEndClass();

    /// <summary>
    /// Returns the generated source code as a string.
    /// </summary>
    string ToSourceText();
}
