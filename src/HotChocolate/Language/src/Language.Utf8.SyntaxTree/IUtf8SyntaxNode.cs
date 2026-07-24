using System.Buffers;

namespace HotChocolate.Language;

/// <summary>
/// Represents a node in the packed UTF-8 syntax tree that can write its own GraphQL source text.
/// </summary>
public interface IUtf8SyntaxNode
{
    /// <summary>
    /// Writes this node's GraphQL source text to the specified buffer writer, substituting
    /// variable names through <paramref name="variables"/>. The output is the node's verbatim
    /// source range, including the original whitespace and comments, with only variable name
    /// tokens replaced. Formatting with an empty map reproduces the original source byte for byte.
    /// </summary>
    /// <param name="writer">
    /// The buffer writer that receives the UTF-8 encoded output.
    /// </param>
    /// <param name="variables">
    /// The ordinal-indexed variable name substitutions to apply, or the default value to keep
    /// every original name.
    /// </param>
    void Format(IBufferWriter<byte> writer, Utf8VariableNameMap variables = default);
}
