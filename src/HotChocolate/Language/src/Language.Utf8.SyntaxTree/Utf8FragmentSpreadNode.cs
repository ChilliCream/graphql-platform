using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Language;

/// <summary>
/// Provides a view over a fragment spread in a packed UTF-8 syntax tree.
/// </summary>
public readonly struct Utf8FragmentSpreadNode : IUtf8SyntaxNode
{
    private readonly Utf8OperationDocument _document;
    private readonly int _cursor;

    internal Utf8FragmentSpreadNode(Utf8OperationDocument document, int cursor)
    {
        // document is usually not null, but the Current property on the enumerators
        // (when initialized as default) can physically pass null.
        _document = document;
        _cursor = cursor;
        Debug.Assert(document.GetRow(cursor).Kind is Utf8SyntaxKind.FragmentSpread);
    }

    /// <summary>
    /// Gets the referenced fragment name.
    /// </summary>
    internal string Name
    {
        get
        {
            CheckValidInstance();
            var nameRow = _document.GetRow(_cursor + 1);
            return _document.GetString(nameRow.Location, nameRow.SizeOrLength);
        }
    }

    /// <summary>
    /// Gets the UTF-8 encoded referenced fragment name.
    /// </summary>
    public ReadOnlySpan<byte> Utf8Name
    {
        get
        {
            CheckValidInstance();
            var nameRow = _document.GetRow(_cursor + 1);
            return _document.GetSource(nameRow.Location, nameRow.SizeOrLength);
        }
    }

    /// <summary>
    /// Writes this fragment spread's GraphQL source text to the specified buffer writer,
    /// substituting variable names through <paramref name="variables"/>.
    /// </summary>
    /// <param name="writer">
    /// The buffer writer that receives the UTF-8 encoded output.
    /// </param>
    /// <param name="indented">
    /// <see langword="false"/>, the default, to write compact single-line output that drops
    /// comments and keeps only the whitespace that is required to separate two tokens;
    /// <see langword="true"/> to preserve the formatting of the source document verbatim,
    /// including its whitespace and comments.
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
    public void Format(
        IBufferWriter<byte> writer,
        bool indented = false,
        bool formatAsJsonStringValue = false,
        Utf8VariableNameMap variables = default)
    {
        CheckValidInstance();
        Utf8SyntaxFormatter.Format(
            _document, _cursor, writer, indented, formatAsJsonStringValue, variables);
    }

    private void CheckValidInstance()
    {
        if (_document is null)
        {
            throw new InvalidOperationException();
        }
    }
}
