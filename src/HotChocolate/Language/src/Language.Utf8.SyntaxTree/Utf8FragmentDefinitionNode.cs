using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Language;

/// <summary>
/// Provides a view over a fragment definition in a packed UTF-8 syntax tree.
/// </summary>
public readonly struct Utf8FragmentDefinitionNode : IUtf8SyntaxNode
{
    private readonly Utf8OperationDocument _document;
    private readonly int _cursor;

    internal Utf8FragmentDefinitionNode(Utf8OperationDocument document, int cursor)
    {
        // document is usually not null, but the Current property on the enumerators
        // (when initialized as default) can physically pass null.
        _document = document;
        _cursor = cursor;
        Debug.Assert(document.GetRow(cursor).Kind is Utf8SyntaxKind.FragmentDefinition);
    }

    /// <summary>
    /// Gets the fragment name.
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
    /// Gets the UTF-8 encoded fragment name.
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
    /// Gets the fragment type condition.
    /// </summary>
    internal string TypeCondition
    {
        get
        {
            CheckValidInstance();
            var typeRow = _document.GetRow(_document.SkipVariableDefinitions(_cursor + 2));
            return _document.GetString(typeRow.Location, typeRow.SizeOrLength);
        }
    }

    /// <summary>
    /// Gets the UTF-8 encoded fragment type condition.
    /// </summary>
    public ReadOnlySpan<byte> Utf8TypeCondition
    {
        get
        {
            CheckValidInstance();
            var typeRow = _document.GetRow(_document.SkipVariableDefinitions(_cursor + 2));
            return _document.GetSource(typeRow.Location, typeRow.SizeOrLength);
        }
    }

    /// <summary>
    /// Gets the variable definitions declared by this fragment.
    /// </summary>
    public Utf8VariableDefinitionEnumerable GetVariableDefinitions()
    {
        CheckValidInstance();
        return new Utf8VariableDefinitionEnumerable(_document, _cursor + 2);
    }

    /// <summary>
    /// Gets the root selection set of this fragment.
    /// </summary>
    public Utf8SelectionSetNode SelectionSet
    {
        get
        {
            CheckValidInstance();
            return new Utf8SelectionSetNode(
                _document,
                _document.SkipVariableDefinitions(_cursor + 2) + 1);
        }
    }

    /// <summary>
    /// Writes this fragment's GraphQL source text to the specified buffer writer, substituting
    /// variable names through <paramref name="variables"/>.
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
/// Provides pattern-based enumeration over fragment definitions without collection interfaces.
/// </summary>
public readonly struct Utf8FragmentDefinitionEnumerable
{
    private readonly Utf8OperationDocument? _document;

    internal Utf8FragmentDefinitionEnumerable(Utf8OperationDocument document)
    {
        _document = document;
    }

    /// <summary>
    /// Gets an enumerator over the fragment definitions.
    /// </summary>
    public Enumerator GetEnumerator() => new(_document);

    /// <summary>
    /// Enumerates fragment definitions without implementing enumerator interfaces.
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
        /// Gets the current fragment definition.
        /// </summary>
        public readonly Utf8FragmentDefinitionNode Current
            => _document is null || _current < 0
                ? default
                : new Utf8FragmentDefinitionNode(_document, _current);

        /// <summary>
        /// Advances to the next fragment definition.
        /// </summary>
        public bool MoveNext()
        {
            while (_document is not null && _next < _document.RowCount)
            {
                var cursor = _next;
                var row = _document.GetRow(cursor);
                _next = checked(cursor + row.NumberOfRows);

                if (row.Kind is Utf8SyntaxKind.FragmentDefinition)
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
