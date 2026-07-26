using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Language;

/// <summary>
/// Provides a view over a field selection in a packed UTF-8 syntax tree.
/// </summary>
public readonly struct Utf8FieldNode : IUtf8SyntaxNode
{
    private readonly Utf8OperationDocument _document;
    private readonly int _cursor;

    internal Utf8FieldNode(Utf8OperationDocument document, int cursor)
    {
        // document is usually not null, but the Current property on the enumerators
        // (when initialized as default) can physically pass null.
        _document = document;
        _cursor = cursor;
        Debug.Assert(document.GetRow(cursor).Kind is Utf8SyntaxKind.Field);
    }

    /// <summary>
    /// Gets the document this field belongs to.
    /// </summary>
    internal Utf8OperationDocument Document => _document;

    /// <summary>
    /// Gets the field name.
    /// </summary>
    internal string Name
    {
        get
        {
            CheckValidInstance();
            var nameRow = _document.GetRow(NameCursor());
            return _document.GetString(nameRow.Location, nameRow.SizeOrLength);
        }
    }

    /// <summary>
    /// Gets the UTF-8 encoded field name.
    /// </summary>
    public ReadOnlySpan<byte> Utf8Name
    {
        get
        {
            CheckValidInstance();
            var nameRow = _document.GetRow(NameCursor());
            return _document.GetSource(nameRow.Location, nameRow.SizeOrLength);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this field has an alias.
    /// </summary>
    public bool HasAlias
    {
        get
        {
            CheckValidInstance();
            return _document.GetRow(_cursor + 1).Kind is Utf8SyntaxKind.Alias;
        }
    }

    /// <summary>
    /// Gets the field alias, or <see langword="null"/> when no alias is declared.
    /// </summary>
    internal string? Alias
    {
        get
        {
            CheckValidInstance();
            var aliasRow = _document.GetRow(_cursor + 1);
            return aliasRow.Kind is Utf8SyntaxKind.Alias
                ? _document.GetString(aliasRow.Location, aliasRow.SizeOrLength)
                : null;
        }
    }

    /// <summary>
    /// Gets the UTF-8 encoded field alias.
    /// </summary>
    public ReadOnlySpan<byte> Utf8Alias
    {
        get
        {
            CheckValidInstance();
            var aliasRow = _document.GetRow(_cursor + 1);
            return aliasRow.Kind is Utf8SyntaxKind.Alias
                ? _document.GetSource(aliasRow.Location, aliasRow.SizeOrLength)
                : [];
        }
    }

    /// <summary>
    /// Gets a value indicating whether this field has a selection set.
    /// </summary>
    public bool HasSelectionSet
    {
        get
        {
            CheckValidInstance();
            return SelectionSetCursor() >= 0;
        }
    }

    /// <summary>
    /// Gets the field selection set.
    /// </summary>
    public Utf8SelectionSetNode SelectionSet
    {
        get
        {
            CheckValidInstance();
            var cursor = SelectionSetCursor();
            return cursor >= 0
                ? new Utf8SelectionSetNode(_document, cursor)
                : throw new InvalidOperationException("The field has no selection set.");
        }
    }

    /// <summary>
    /// Writes this field's GraphQL source text to the specified buffer writer, substituting
    /// variable names through <paramref name="variables"/>.
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

    /// <summary>
    /// Writes this field's GraphQL source text to the specified syntax writer in compact form,
    /// inserting <paramref name="variablePrefix"/> in front of every variable name whose ordinal
    /// is not in <paramref name="shared"/>. The delimiters of a JSON string value are not written.
    /// </summary>
    /// <param name="writer">
    /// The syntax writer that receives the UTF-8 encoded output.
    /// </param>
    /// <param name="variablePrefix">
    /// The bytes inserted in front of each renamed variable name.
    /// </param>
    /// <param name="shared">
    /// The ordinals of the variables that keep their original name.
    /// </param>
    internal void Format(
        Utf8SyntaxWriter writer,
        ReadOnlySpan<byte> variablePrefix,
        SharedOrdinalSet shared)
    {
        CheckValidInstance();
        Utf8SyntaxFormatter.Write(_document, _cursor, writer, variablePrefix, shared);
    }

    private int NameCursor()
    {
        var next = _cursor + 1;
        return _document.GetRow(next).Kind is Utf8SyntaxKind.Alias ? next + 1 : next;
    }

    private int SelectionSetCursor()
        => _document.FindSelectionSet(
            NameCursor() + 1,
            _cursor + _document.GetRow(_cursor).NumberOfRows);

    private void CheckValidInstance()
    {
        if (_document is null)
        {
            throw new InvalidOperationException();
        }
    }
}
