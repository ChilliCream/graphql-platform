using System.Buffers;

namespace HotChocolate.Language;

/// <summary>
/// Represents a node in the packed UTF-8 syntax tree that can write its own GraphQL source text.
/// </summary>
public interface IUtf8SyntaxNode
{
    /// <summary>
    /// Writes this node's GraphQL source text to the specified buffer writer, substituting
    /// variable names through <paramref name="variables"/>. String and block string literals are
    /// preserved in either formatting mode.
    /// </summary>
    /// <param name="writer">
    /// The buffer writer that receives the UTF-8 encoded output.
    /// </param>
    /// <param name="indented">
    /// <see langword="false"/>, the default, to write compact single-line output that drops
    /// comments and keeps only the whitespace that is required to separate two tokens;
    /// <see langword="true"/> to preserve the formatting of the source document verbatim,
    /// including its whitespace and comments, which reproduces the node's verbatim source range.
    /// Formatting verbatim with an empty map reproduces the original source byte for byte.
    /// </param>
    /// <param name="formatAsJsonStringValue">
    /// <see langword="false"/>, the default, to write plain GraphQL source text;
    /// <see langword="true"/> to write a JSON string value that holds the GraphQL source text,
    /// including the enclosing quotation marks.
    /// </param>
    /// <param name="variables">
    /// The ordinal-indexed variable name substitutions to apply, or the default value to keep
    /// every original name.
    /// </param>
    void Format(
        IBufferWriter<byte> writer,
        bool indented = false,
        bool formatAsJsonStringValue = false,
        Utf8VariableNameMap variables = default);
}
