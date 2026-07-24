using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Language;

/// <summary>
/// Provides a view over a selection set in a packed UTF-8 syntax tree.
/// </summary>
public readonly struct Utf8SelectionSetNode : IUtf8SyntaxNode
{
    private readonly Utf8OperationDocument _document;
    private readonly int _cursor;

    internal Utf8SelectionSetNode(Utf8OperationDocument document, int cursor)
    {
        // document is usually not null, but the Current property on the enumerators
        // (when initialized as default) can physically pass null.
        _document = document;
        _cursor = cursor;
        Debug.Assert(document.GetRow(cursor).Kind is Utf8SyntaxKind.SelectionSet);
    }

    /// <summary>
    /// Gets the selections in this selection set.
    /// </summary>
    public Utf8SelectionEnumerable GetSelections()
    {
        CheckValidInstance();
        var row = _document.GetRow(_cursor);
        return new Utf8SelectionEnumerable(_document, _cursor + 1, _cursor + row.NumberOfRows);
    }

    /// <summary>
    /// Writes this selection set's GraphQL source text to the specified buffer writer,
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
/// Provides pattern-based enumeration over selections without collection interfaces.
/// </summary>
public readonly struct Utf8SelectionEnumerable
{
    private readonly Utf8OperationDocument? _document;
    private readonly int _start;
    private readonly int _end;

    internal Utf8SelectionEnumerable(
        Utf8OperationDocument document,
        int start,
        int end)
    {
        _document = document;
        _start = start;
        _end = end;
    }

    /// <summary>
    /// Gets an enumerator over the selections.
    /// </summary>
    public Enumerator GetEnumerator() => new(_document, _start, _end);

    /// <summary>
    /// Enumerates selections without implementing enumerator interfaces.
    /// </summary>
    public struct Enumerator
    {
        private readonly Utf8OperationDocument? _document;
        private readonly int _end;
        private int _next;
        private int _current;

        internal Enumerator(Utf8OperationDocument? document, int start, int end)
        {
            _document = document;
            _next = start;
            _end = end;
            _current = -1;
        }

        /// <summary>
        /// Gets the current selection.
        /// </summary>
        public Utf8SelectionNode Current
            => _document is null || _current < 0
                ? default
                : new Utf8SelectionNode(_document, _current);

        /// <summary>
        /// Advances to the next selection.
        /// </summary>
        public bool MoveNext()
        {
            if (_document is null || _next >= _end)
            {
                _current = -1;
                return false;
            }

            _current = _next;
            _next = checked(_next + _document.GetRow(_next).NumberOfRows);
            return true;
        }
    }
}
