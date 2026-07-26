using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Language;

/// <summary>
/// Provides a view over an operation definition in a packed UTF-8 syntax tree.
/// </summary>
public readonly struct Utf8OperationDefinitionNode : IUtf8SyntaxNode
{
    private readonly Utf8OperationDocument _document;
    private readonly int _cursor;

    internal Utf8OperationDefinitionNode(Utf8OperationDocument document, int cursor)
    {
        // document is usually not null, but the Current property on the enumerators
        // (when initialized as default) can physically pass null.
        _document = document;
        _cursor = cursor;
        Debug.Assert(document.GetRow(cursor).Kind
            is Utf8SyntaxKind.OperationQuery
            or Utf8SyntaxKind.OperationMutation
            or Utf8SyntaxKind.OperationSubscription);
    }

    /// <summary>
    /// Gets a value indicating whether this operation has a name.
    /// </summary>
    public bool HasName
    {
        get
        {
            CheckValidInstance();
            return _document.GetRow(_cursor + 1).Kind is Utf8SyntaxKind.Name;
        }
    }

    /// <summary>
    /// Gets the operation name, or <see langword="null"/> when the operation is anonymous.
    /// </summary>
    internal string? Name
    {
        get
        {
            CheckValidInstance();
            var nameRow = _document.GetRow(_cursor + 1);
            return nameRow.Kind is Utf8SyntaxKind.Name
                ? _document.GetString(nameRow.Location, nameRow.SizeOrLength)
                : null;
        }
    }

    /// <summary>
    /// Gets the UTF-8 encoded operation name.
    /// </summary>
    public ReadOnlySpan<byte> Utf8Name
    {
        get
        {
            CheckValidInstance();
            var nameRow = _document.GetRow(_cursor + 1);
            return nameRow.Kind is Utf8SyntaxKind.Name
                ? _document.GetSource(nameRow.Location, nameRow.SizeOrLength)
                : [];
        }
    }

    /// <summary>
    /// Gets the operation type.
    /// </summary>
    public OperationType Operation
    {
        get
        {
            CheckValidInstance();
            return _document.GetRow(_cursor).Kind switch
            {
                Utf8SyntaxKind.OperationMutation => OperationType.Mutation,
                Utf8SyntaxKind.OperationSubscription => OperationType.Subscription,
                _ => OperationType.Query
            };
        }
    }

    /// <summary>
    /// Gets the variable definitions declared by this operation.
    /// </summary>
    public Utf8VariableDefinitionEnumerable GetVariableDefinitions()
    {
        CheckValidInstance();
        return new Utf8VariableDefinitionEnumerable(_document, VariableStart());
    }

    /// <summary>
    /// Gets the root selection set of this operation.
    /// </summary>
    public Utf8SelectionSetNode SelectionSet
    {
        get
        {
            CheckValidInstance();
            return new Utf8SelectionSetNode(
                _document,
                _document.FindSelectionSet(
                    VariableStart(),
                    _cursor + _document.GetRow(_cursor).NumberOfRows));
        }
    }

    /// <summary>
    /// Writes this operation's GraphQL source text to the specified buffer writer, substituting
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

    private int VariableStart()
    {
        var next = _cursor + 1;
        return _document.GetRow(next).Kind is Utf8SyntaxKind.Name ? next + 1 : next;
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
/// Provides pattern-based enumeration over operation definitions without collection interfaces.
/// </summary>
public readonly struct Utf8OperationDefinitionEnumerable
{
    private readonly Utf8OperationDocument? _document;

    internal Utf8OperationDefinitionEnumerable(Utf8OperationDocument document)
    {
        _document = document;
    }

    /// <summary>
    /// Gets an enumerator over the operation definitions.
    /// </summary>
    public Enumerator GetEnumerator() => new(_document);

    /// <summary>
    /// Enumerates operation definitions without implementing enumerator interfaces.
    /// </summary>
    public struct Enumerator
    {
        private readonly Utf8OperationDocument? _document;
        private int _next;
        private int _current;

        internal Enumerator(Utf8OperationDocument? document)
        {
            _document = document;
            _next = 0;
            _current = -1;
        }

        /// <summary>
        /// Gets the current operation definition.
        /// </summary>
        public readonly Utf8OperationDefinitionNode Current
            => _document is null || _current < 0
                ? default
                : new Utf8OperationDefinitionNode(_document, _current);

        /// <summary>
        /// Advances to the next operation definition.
        /// </summary>
        public bool MoveNext()
        {
            while (_document is not null && _next < _document.RowCount)
            {
                var cursor = _next;
                var row = _document.GetRow(cursor);
                _next = checked(cursor + row.NumberOfRows);

                if (row.Kind
                    is Utf8SyntaxKind.OperationQuery
                    or Utf8SyntaxKind.OperationMutation
                    or Utf8SyntaxKind.OperationSubscription)
                {
                    _current = cursor;
                    return true;
                }
            }

            _current = -1;
            return false;
        }
    }
}
