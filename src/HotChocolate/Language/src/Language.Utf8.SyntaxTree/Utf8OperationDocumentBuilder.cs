using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace HotChocolate.Language;

internal struct Utf8OperationDocumentBuilder
{
    private const int InitialCapacity = 4;
    private const int PrefixBufferLength = 12;
    private const int StackWordThreshold = 64;
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    private readonly IBufferWriter<byte> _writer;
    private Utf8FieldNode[]? _items;
    private int _itemCount;
    private Utf8VariableDefinitionNode[]? _sharedDefinitions;
    private int _sharedCount;
    private WriterStage _stage;
    private bool _keywordWritten;
    private bool _parenOpened;

    private Utf8OperationDocumentBuilder(IBufferWriter<byte> writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Creates a new builder that streams a batched lookup operation into <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">
    /// The buffer writer that receives the UTF-8 encoded output.
    /// </param>
    /// <returns>
    /// The new builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="writer"/> is <see langword="null"/>.
    /// </exception>
    internal static Utf8OperationDocumentBuilder New(IBufferWriter<byte> writer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        return new Utf8OperationDocumentBuilder(writer);
    }

    /// <summary>
    /// Sets the name of the operation and writes it immediately. The name must be set before any
    /// shared variable or root selection is added.
    /// </summary>
    /// <param name="name">
    /// The operation name.
    /// </param>
    /// <returns>
    /// The builder for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The name is set out of order.
    /// </exception>
    internal Utf8OperationDocumentBuilder SetName(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        EnsureCanSetName();
        EnsureKeyword();
        _writer.Write(" "u8);

        var byteCount = s_utf8.GetByteCount(name);
        var span = _writer.GetSpan(byteCount);
        var written = s_utf8.GetBytes(name, span);
        _writer.Advance(written);

        _stage = WriterStage.Named;
        return this;
    }

    /// <summary>
    /// Sets the name of the operation and writes it immediately. The name must be set before any
    /// shared variable or root selection is added.
    /// </summary>
    /// <param name="name">
    /// The UTF-8 encoded operation name. The bytes are written immediately and need not outlive
    /// the call.
    /// </param>
    /// <returns>
    /// The builder for chaining.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The name is set out of order.
    /// </exception>
    internal Utf8OperationDocumentBuilder SetName(ReadOnlySpan<byte> name)
    {
        EnsureCanSetName();
        EnsureKeyword();
        _writer.Write(" "u8);
        _writer.Write(name);

        _stage = WriterStage.Named;
        return this;
    }

    /// <summary>
    /// Declares a variable that is shared across all items and writes its definition immediately.
    /// A shared variable is declared once and keeps its original name, so items that use it are not
    /// prefixed. Shared variables must be added before any root selection.
    /// </summary>
    /// <param name="definition">
    /// The variable definition to declare. Its source range is written verbatim.
    /// </param>
    /// <returns>
    /// The builder for chaining.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The shared variable is added out of order, or <paramref name="definition"/> is not
    /// associated with a document.
    /// </exception>
    internal Utf8OperationDocumentBuilder AddSharedVariable(Utf8VariableDefinitionNode definition)
    {
        EnsureCanAddSharedVariable();

        // Reading the name validates the view before any bytes are written.
        _ = definition.Utf8Name;

        EnsureKeyword();
        WriteDefinitionSeparator();
        definition.Format(_writer);

        if (_sharedDefinitions is null || _sharedDefinitions.Length == _sharedCount)
        {
            GrowSharedDefinitions();
        }

        _sharedDefinitions![_sharedCount++] = definition;
        _stage = WriterStage.SharedVariables;
        return this;
    }

    /// <summary>
    /// Adds a root selection to the batched operation and writes its renamed variable definitions
    /// immediately. The field is emitted as an aliased copy whose variables are renamed per item,
    /// and its body is written when the operation is completed.
    /// </summary>
    /// <param name="selection">
    /// The root field selection to add.
    /// </param>
    /// <returns>
    /// The builder for chaining.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The selection is added after the operation is completed, the selection is not associated
    /// with a document, or the source document does not contain exactly one operation.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// <paramref name="selection"/> carries an alias, or its source document contains a fragment
    /// spread. Batched lookup selections must be plain fields, and fragment spreads are not
    /// supported.
    /// </exception>
    internal Utf8OperationDocumentBuilder AddRootSelection(Utf8FieldNode selection)
    {
        EnsureCanAddRootSelection();

        var document = selection.Document;

        if (document is null)
        {
            throw new InvalidOperationException(
                "The root selection is not associated with a document.");
        }

        if (selection.HasAlias)
        {
            throw new NotSupportedException(
                "A batched lookup root selection must be a plain field without an alias.");
        }

        if (IsNewDocument(document) && ContainsFragmentSpread(document))
        {
            throw new NotSupportedException(
                "Fragment spreads are not supported in batched lookup selections.");
        }

        var operationCursor = FindSingleOperationCursor(document);

        EnsureKeyword();
        WriteItemVariableDefinitions(document, operationCursor, _itemCount);

        if (_items is null || _items.Length == _itemCount)
        {
            GrowItems();
        }

        _items![_itemCount++] = selection;
        _stage = WriterStage.RootSelections;
        return this;
    }

    /// <summary>
    /// Writes the item bodies and closes the batched lookup operation. The builder is single use,
    /// and a second call throws.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The operation has already been completed, or no root selection was added.
    /// </exception>
    internal void Complete()
    {
        EnsureCanComplete();
        _stage = WriterStage.Completed;

        ulong[]? rentedBits = null;
        try
        {
            if (_parenOpened)
            {
                _writer.Write(")"u8);
            }

            _writer.Write(" { "u8);

            var maxVariableCount = MaxVariableCount();
            var wordCount = (maxVariableCount + 63) >> 6;
            var bits = maxVariableCount <= StackWordThreshold
                ? stackalloc ulong[1]
                : (rentedBits = ArrayPool<ulong>.Shared.Rent(wordCount)).AsSpan(0, wordCount);

            Span<byte> prefixBuffer = stackalloc byte[PrefixBufferLength];

            for (var i = 0; i < _itemCount; i++)
            {
                if (i > 0)
                {
                    _writer.Write(" "u8);
                }

                WriteItem(i, prefixBuffer, bits);
            }

            _writer.Write(" }"u8);
        }
        finally
        {
            if (rentedBits is not null)
            {
                ArrayPool<ulong>.Shared.Return(rentedBits);
            }

            ReleaseBuffers();
        }
    }

    private readonly void WriteItem(int index, Span<byte> prefixBuffer, Span<ulong> bits)
    {
        var field = _items![index];
        var document = field.Document;
        var prefix = ComposePrefix(prefixBuffer, index);

        _writer.Write(prefix);
        _writer.Write(field.Utf8Name);
        _writer.Write(": "u8);

        var variableCount = document.VariableCount;
        var shared = new SharedOrdinalSet(bits.Slice(0, (variableCount + 63) >> 6));

        for (var ordinal = 0; ordinal < variableCount; ordinal++)
        {
            if (ContainsSharedDefinition(document.GetVariableName(ordinal)))
            {
                shared.Add(ordinal);
            }
        }

        field.Format(_writer, prefix, shared);
    }

    private void WriteItemVariableDefinitions(
        Utf8OperationDocument document,
        int operationCursor,
        int index)
    {
        Span<byte> prefixBuffer = stackalloc byte[PrefixBufferLength];
        var prefix = ComposePrefix(prefixBuffer, index);

        var cursor = VariableStart(document, operationCursor);
        var rowCount = document.RowCount;

        while (cursor < rowCount
            && document.GetRow(cursor).Kind is Utf8SyntaxKind.VariableDefinition)
        {
            var definitionRow = document.GetRow(cursor);
            var nameRow = document.GetRow(cursor + 1);
            var name = document.GetSource(nameRow.Location, nameRow.SizeOrLength);

            if (!ContainsSharedDefinition(name))
            {
                WriteDefinitionSeparator();

                // Insert the item prefix in front of the name; the original name and everything
                // before it, including a leading description and the dollar sign, stay verbatim.
                _writer.Write(document.GetSource(
                    definitionRow.Location,
                    nameRow.Location - definitionRow.Location));
                _writer.Write(prefix);
                _writer.Write(document.GetSource(
                    nameRow.Location,
                    definitionRow.SourceEnd - nameRow.Location));
            }

            cursor += definitionRow.NumberOfRows;
        }
    }

    private void WriteDefinitionSeparator()
    {
        if (_parenOpened)
        {
            _writer.Write(", "u8);
        }
        else
        {
            _writer.Write("("u8);
            _parenOpened = true;
        }
    }

    private void EnsureKeyword()
    {
        if (!_keywordWritten)
        {
            _writer.Write("query"u8);
            _keywordWritten = true;
        }
    }

    private readonly void EnsureCanSetName()
    {
        if (_stage != WriterStage.New)
        {
            throw new InvalidOperationException(
                "The operation name must be set before shared variables and root selections "
                + "are added.");
        }
    }

    private readonly void EnsureCanAddSharedVariable()
    {
        if (_stage > WriterStage.SharedVariables)
        {
            throw new InvalidOperationException(
                "Shared variables must be added before root selections and before the operation "
                + "is completed.");
        }
    }

    private readonly void EnsureCanAddRootSelection()
    {
        if (_stage > WriterStage.RootSelections)
        {
            throw new InvalidOperationException(
                "Root selections cannot be added after the operation has been completed.");
        }
    }

    private readonly void EnsureCanComplete()
    {
        if (_stage == WriterStage.Completed)
        {
            throw new InvalidOperationException("The operation has already been completed.");
        }

        if (_stage != WriterStage.RootSelections)
        {
            throw new InvalidOperationException(
                "A batched lookup operation requires at least one root selection.");
        }
    }

    private readonly bool ContainsSharedDefinition(ReadOnlySpan<byte> name)
    {
        for (var i = 0; i < _sharedCount; i++)
        {
            if (_sharedDefinitions![i].Utf8Name.SequenceEqual(name))
            {
                return true;
            }
        }

        return false;
    }

    private readonly bool IsNewDocument(Utf8OperationDocument document)
    {
        for (var i = 0; i < _itemCount; i++)
        {
            if (ReferenceEquals(_items![i].Document, document))
            {
                return false;
            }
        }

        return true;
    }

    private readonly int MaxVariableCount()
    {
        var max = 0;
        for (var i = 0; i < _itemCount; i++)
        {
            var count = _items![i].Document.VariableCount;
            if (count > max)
            {
                max = count;
            }
        }

        return max;
    }

    private static ReadOnlySpan<byte> ComposePrefix(Span<byte> buffer, int index)
    {
        buffer[0] = (byte)'_';
        Utf8Formatter.TryFormat(index, buffer.Slice(1), out var written);
        buffer[written + 1] = (byte)'_';
        return buffer.Slice(0, written + 2);
    }

    private static bool ContainsFragmentSpread(Utf8OperationDocument document)
    {
        var rowCount = document.RowCount;
        for (var i = 0; i < rowCount; i++)
        {
            if (document.GetRow(i).Kind is Utf8SyntaxKind.FragmentSpread)
            {
                return true;
            }
        }

        return false;
    }

    private static int FindSingleOperationCursor(Utf8OperationDocument document)
    {
        var operationCursor = -1;
        var index = 0;
        var rowCount = document.RowCount;

        while (index < rowCount)
        {
            var row = document.GetRow(index);
            if (row.Kind is Utf8SyntaxKind.OperationQuery
                or Utf8SyntaxKind.OperationMutation
                or Utf8SyntaxKind.OperationSubscription)
            {
                if (operationCursor >= 0)
                {
                    throw new InvalidOperationException(
                        "A batched lookup source document must contain exactly one operation.");
                }

                operationCursor = index;
            }

            index += row.NumberOfRows;
        }

        if (operationCursor < 0)
        {
            throw new InvalidOperationException(
                "A batched lookup source document must contain exactly one operation.");
        }

        return operationCursor;
    }

    private static int VariableStart(Utf8OperationDocument document, int operationCursor)
    {
        var next = operationCursor + 1;
        return document.GetRow(next).Kind is Utf8SyntaxKind.Name ? next + 1 : next;
    }

    private void GrowItems()
    {
        var capacity = _items is null ? InitialCapacity : _items.Length * 2;
        var next = ArrayPool<Utf8FieldNode>.Shared.Rent(capacity);

        if (_items is not null)
        {
            Array.Copy(_items, next, _itemCount);
            ArrayPool<Utf8FieldNode>.Shared.Return(_items, clearArray: true);
        }

        _items = next;
    }

    private void GrowSharedDefinitions()
    {
        var capacity = _sharedDefinitions is null ? InitialCapacity : _sharedDefinitions.Length * 2;
        var next = ArrayPool<Utf8VariableDefinitionNode>.Shared.Rent(capacity);

        if (_sharedDefinitions is not null)
        {
            Array.Copy(_sharedDefinitions, next, _sharedCount);
            ArrayPool<Utf8VariableDefinitionNode>.Shared.Return(_sharedDefinitions, clearArray: true);
        }

        _sharedDefinitions = next;
    }

    private void ReleaseBuffers()
    {
        if (_items is not null)
        {
            ArrayPool<Utf8FieldNode>.Shared.Return(_items, clearArray: true);
            _items = null;
        }

        if (_sharedDefinitions is not null)
        {
            ArrayPool<Utf8VariableDefinitionNode>.Shared.Return(_sharedDefinitions, clearArray: true);
            _sharedDefinitions = null;
        }

        _itemCount = 0;
        _sharedCount = 0;
    }

    private enum WriterStage : byte
    {
        New,
        Named,
        SharedVariables,
        RootSelections,
        Completed
    }
}
