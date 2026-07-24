using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Language;

/// <summary>
/// Provides a view over a variable definition in a packed UTF-8 syntax tree.
/// </summary>
public readonly struct Utf8VariableDefinitionNode : IUtf8SyntaxNode
{
    private readonly Utf8OperationDocument _document;
    private readonly int _cursor;

    internal Utf8VariableDefinitionNode(Utf8OperationDocument document, int cursor)
    {
        // document is usually not null, but the Current property on the enumerators
        // (when initialized as default) can physically pass null.
        _document = document;
        _cursor = cursor;
        Debug.Assert(document.GetRow(cursor).Kind is Utf8SyntaxKind.VariableDefinition);
    }

    /// <summary>
    /// Gets the variable name without the leading dollar sign.
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
    /// Gets the UTF-8 encoded variable name without the leading dollar sign.
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
    /// Gets the document-scoped ordinal of this variable. Ordinals identify distinct variable
    /// names and are assigned by first occurrence in document order.
    /// </summary>
    public int Ordinal
    {
        get
        {
            CheckValidInstance();
            var nameRow = _document.GetRow(_cursor + 1);
            var name = _document.GetSource(nameRow.Location, nameRow.SizeOrLength);
            var count = _document.VariableCount;

            for (var ordinal = 0; ordinal < count; ordinal++)
            {
                if (name.SequenceEqual(_document.GetVariableName(ordinal)))
                {
                    return ordinal;
                }
            }

            throw new InvalidOperationException(
                "The variable definition is not present in the document's variable directory.");
        }
    }

    /// <summary>
    /// Writes this variable definition's GraphQL source text to the specified buffer writer,
    /// substituting variable names through <paramref name="variables"/>.
    /// </summary>
    /// <param name="writer">
    /// The buffer writer that receives the UTF-8 encoded output.
    /// </param>
    /// <param name="variables">
    /// The ordinal-indexed variable name substitutions to apply, or the default value to keep
    /// every original name.
    /// </param>
    public void Format(IBufferWriter<byte> writer, Utf8VariableNameMap variables = default)
    {
        CheckValidInstance();
        Utf8SyntaxFormatter.Write(_document, _cursor, writer, variables);
    }

    private void CheckValidInstance()
    {
        if (_document is null)
        {
            throw new InvalidOperationException();
        }
    }
}

/// <summary>
/// Provides pattern-based enumeration over variable definitions without collection interfaces.
/// </summary>
public readonly struct Utf8VariableDefinitionEnumerable
{
    private readonly Utf8OperationDocument? _document;
    private readonly int _start;

    internal Utf8VariableDefinitionEnumerable(
        Utf8OperationDocument document,
        int start)
    {
        _document = document;
        _start = start;
    }

    /// <summary>
    /// Gets an enumerator over the variable definitions.
    /// </summary>
    public Enumerator GetEnumerator() => new(_document, _start);

    /// <summary>
    /// Enumerates variable definitions without implementing enumerator interfaces.
    /// </summary>
    public struct Enumerator
    {
        private readonly Utf8OperationDocument? _document;
        private int _next;
        private int _current;

        internal Enumerator(Utf8OperationDocument? document, int start)
        {
            _document = document;
            _next = start;
            _current = -1;
        }

        /// <summary>
        /// Gets the current variable definition.
        /// </summary>
        public Utf8VariableDefinitionNode Current
            => _document is null || _current < 0
                ? default
                : new Utf8VariableDefinitionNode(_document, _current);

        /// <summary>
        /// Advances to the next variable definition.
        /// </summary>
        public bool MoveNext()
        {
            if (_document is not null
                && _next < _document.RowCount
                && _document.GetRow(_next).Kind is Utf8SyntaxKind.VariableDefinition)
            {
                _current = _next;
                _next = checked(_next + _document.GetRow(_next).NumberOfRows);
                return true;
            }

            _current = -1;
            return false;
        }
    }
}
