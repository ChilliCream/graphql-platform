using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace HotChocolate.Language;

/// <summary>
/// Builds a batched lookup operation from root field selections of packed UTF-8 syntax trees and
/// streams it to a buffer writer. Each added selection becomes an aliased root field whose
/// variables are renamed per item, unless the variable is declared as shared.
/// </summary>
internal struct Utf8OperationDocumentBuilder
{
    private const int InitialCapacity = 4;
    private const int PrefixBufferLength = 12;
    private const int OrdinalBufferLength = 10;
    private const int StackWordThreshold = 64;

    private readonly Utf8SyntaxWriter _writer;
    private Item[]? _items;
    private int _itemCount;
    private SelectionBody[]? _bodies;
    private int _bodyCount;
    private Utf8VariableDefinitionNode[]? _sharedDefinitions;
    private int _sharedCount;
    private WriterStage _stage;
    private bool _opened;
    private bool _keywordWritten;
    private bool _parenOpened;

    private Utf8OperationDocumentBuilder(IBufferWriter<byte> writer, bool formatAsJsonStringValue)
        => _writer = new Utf8SyntaxWriter(writer, formatAsJsonStringValue);

    /// <summary>
    /// Creates a new builder that writes a batched lookup operation to <paramref name="writer"/>.
    /// </summary>
    /// <remarks>
    /// When a call fails after the first content byte was written, the destination buffer holds a
    /// partial operation and must be discarded.
    /// </remarks>
    /// <param name="writer">
    /// The buffer writer that receives the UTF-8 encoded output.
    /// </param>
    /// <param name="formatAsJsonStringValue">
    /// <see langword="false"/>, the default, to write plain GraphQL source text;
    /// <see langword="true"/> to write a JSON string value that holds the GraphQL source text,
    /// including the enclosing quotation marks.
    /// </param>
    /// <returns>
    /// The new builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="writer"/> is <see langword="null"/>.
    /// </exception>
    internal static Utf8OperationDocumentBuilder New(
        IBufferWriter<byte> writer,
        bool formatAsJsonStringValue = false)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        return new Utf8OperationDocumentBuilder(writer, formatAsJsonStringValue);
    }

    /// <summary>
    /// Sets the name of the operation and writes it immediately. The name must be set before any
    /// shared variable or root selection is added.
    /// </summary>
    /// <param name="name">
    /// The operation name, which must be a valid GraphQL name.
    /// </param>
    /// <returns>
    /// The builder for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The name was already set, or a shared variable or root selection was already added.
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
        _writer.Write(name);

        _stage = WriterStage.Named;
        return this;
    }

    /// <summary>
    /// Sets the name of the operation and writes it immediately. The name must be set before any
    /// shared variable or root selection is added.
    /// </summary>
    /// <param name="name">
    /// The UTF-8 encoded operation name, which must be a valid GraphQL name and need not outlive
    /// the call.
    /// </param>
    /// <returns>
    /// The builder for chaining.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The name was already set, or a shared variable or root selection was already added.
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
    /// A shared variable is declared once and keeps its original name in every item that uses it,
    /// and shared variables must be added before any root selection.
    /// </summary>
    /// <param name="definition">
    /// The variable definition to declare, which is written in compact form.
    /// </param>
    /// <returns>
    /// The builder for chaining.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// A root selection was already added, the operation was already completed, or
    /// <paramref name="definition"/> is not associated with a document.
    /// </exception>
    internal Utf8OperationDocumentBuilder AddSharedVariable(Utf8VariableDefinitionNode definition)
    {
        EnsureCanAddSharedVariable();

        // Reading the name validates the view before any bytes are written.
        _ = definition.Utf8Name;

        EnsureKeyword();
        WriteDefinitionSeparator();
        definition.Format(_writer, indented: false);

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
    /// The root field selection to add, which must be a plain field without an alias.
    /// </param>
    /// <param name="typeCondition">
    /// The name of the type the body of <paramref name="selection"/> is selected on, which must be
    /// a valid GraphQL name and must match the type condition of every other item that shares the
    /// body.
    /// </param>
    /// <returns>
    /// The builder for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="typeCondition"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="typeCondition"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The selection is added after the operation is completed, the selection is not associated
    /// with a document, the source document does not contain exactly one operation, or a body that
    /// was already added declares a different type condition.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// <paramref name="selection"/> carries an alias, or its source document contains a fragment
    /// spread.
    /// </exception>
    internal Utf8OperationDocumentBuilder AddRootSelection(
        Utf8FieldNode selection,
        string typeCondition)
    {
        if (typeCondition is null)
        {
            throw new ArgumentNullException(nameof(typeCondition));
        }

        if (typeCondition.Length == 0)
        {
            throw new ArgumentException(
                "The type condition must not be empty.",
                nameof(typeCondition));
        }

        return AddRootSelection(selection, typeCondition, default);
    }

    /// <summary>
    /// Adds a root selection to the batched operation and writes its renamed variable definitions
    /// immediately. The field is emitted as an aliased copy whose variables are renamed per item,
    /// and its body is written when the operation is completed.
    /// </summary>
    /// <param name="selection">
    /// The root field selection to add, which must be a plain field without an alias.
    /// </param>
    /// <param name="typeCondition">
    /// The UTF-8 encoded name of the type the body of <paramref name="selection"/> is selected on,
    /// which must be a valid GraphQL name, must stay valid until <see cref="Complete"/> returns,
    /// and must match the type condition of every other item that shares the body.
    /// </param>
    /// <returns>
    /// The builder for chaining.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="typeCondition"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The selection is added after the operation is completed, the selection is not associated
    /// with a document, the source document does not contain exactly one operation, or a body that
    /// was already added declares a different type condition.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// <paramref name="selection"/> carries an alias, or its source document contains a fragment
    /// spread.
    /// </exception>
    internal Utf8OperationDocumentBuilder AddRootSelection(
        Utf8FieldNode selection,
        ReadOnlyMemory<byte> typeCondition)
    {
        if (typeCondition.IsEmpty)
        {
            throw new ArgumentException(
                "The type condition must not be empty.",
                nameof(typeCondition));
        }

        return AddRootSelection(selection, null, typeCondition);
    }

    private Utf8OperationDocumentBuilder AddRootSelection(
        Utf8FieldNode selection,
        string? typeConditionText,
        ReadOnlyMemory<byte> typeConditionUtf8)
    {
        EnsureCanAddRootSelection();

        var document = selection.Document
            ?? throw new InvalidOperationException(
                "The root selection is not associated with a document.");

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

        // Registering the body is the last step that can fail, so no bytes are written before the
        // item is known to be valid.
        var bodyIndex = RegisterBody(
            document,
            selection.SelectionSetCursor(),
            typeConditionText,
            typeConditionUtf8);

        EnsureKeyword();
        WriteItemVariableDefinitions(document, operationCursor, _itemCount);

        if (_items is null || _items.Length == _itemCount)
        {
            GrowItems();
        }

        _items![_itemCount++] = new Item(selection, bodyIndex);
        _stage = WriterStage.RootSelections;
        return this;
    }

    /// <summary>
    /// Writes the item bodies and the fragments the items share, and closes the batched lookup
    /// operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The operation has already been completed, or no root selection was added.
    /// </exception>
    internal void Complete()
    {
        ulong[]? rentedBits = null;
        try
        {
            // The precondition is checked inside the try so that a builder that cannot be
            // completed still hands back the buffers it rented.
            EnsureCanComplete();
            _stage = WriterStage.Completed;

            EnsureOpened();

            if (_parenOpened)
            {
                _writer.Write(")"u8);
            }

            _writer.Write("{"u8);

            var maxVariableCount = MaxVariableCount();
            var wordCount = (maxVariableCount + 63) >> 6;
            var bits = maxVariableCount <= StackWordThreshold
                ? stackalloc ulong[1]
                : (rentedBits = ArrayPool<ulong>.Shared.Rent(wordCount)).AsSpan(0, wordCount);

            AssignFragmentOrdinals(bits);

            Span<byte> prefixBuffer = stackalloc byte[PrefixBufferLength];
            Span<byte> ordinalBuffer = stackalloc byte[OrdinalBufferLength];

            for (var i = 0; i < _itemCount; i++)
            {
                if (i > 0)
                {
                    _writer.Write(" "u8);
                }

                WriteItem(i, prefixBuffer, ordinalBuffer, bits);
            }

            _writer.Write("}"u8);

            WriteFragmentDefinitions(ordinalBuffer, bits);
            _writer.WriteDelimiter();
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

    /// <summary>
    /// Assigns the ordinal of the generated fragment to every body that more than one item shares
    /// and that uses shared variables only.
    /// </summary>
    private readonly void AssignFragmentOrdinals(Span<ulong> bits)
    {
        var ordinal = 0;

        for (var i = 0; i < _bodyCount; i++)
        {
            ref var body = ref _bodies![i];

            if (body.ItemCount > 1
                && UsesSharedVariablesOnly(body.Document, body.SelectionSetCursor, bits))
            {
                body.FragmentOrdinal = ++ordinal;
            }
        }
    }

    private readonly void WriteItem(
        int index,
        Span<byte> prefixBuffer,
        Span<byte> ordinalBuffer,
        Span<ulong> bits)
    {
        var item = _items![index];
        var field = item.Field;
        var prefix = ComposePrefix(prefixBuffer, index);

        _writer.Write(prefix);
        _writer.Write(field.Utf8Name);
        _writer.Write(":"u8);

        var shared = CreateSharedSet(field.Document, bits);
        var fragmentOrdinal = item.BodyIndex < 0 ? 0 : _bodies![item.BodyIndex].FragmentOrdinal;

        if (fragmentOrdinal == 0)
        {
            field.Format(_writer, prefix, shared);
            return;
        }

        field.FormatWithoutSelectionSet(_writer, prefix, shared);
        _writer.Write("{..."u8);
        WriteFragmentName(fragmentOrdinal, ordinalBuffer);
        _writer.Write("}"u8);
    }

    private readonly void WriteFragmentDefinitions(Span<byte> ordinalBuffer, Span<ulong> bits)
    {
        for (var i = 0; i < _bodyCount; i++)
        {
            ref var body = ref _bodies![i];

            if (body.FragmentOrdinal == 0)
            {
                continue;
            }

            // A fragment body is written without substitution, so every variable it uses must be
            // one that keeps its original name.
            Debug.Assert(UsesSharedVariablesOnly(body.Document, body.SelectionSetCursor, bits));

            _writer.Write(" fragment "u8);
            WriteFragmentName(body.FragmentOrdinal, ordinalBuffer);
            _writer.Write(" on "u8);
            body.WriteTypeCondition(_writer);

            Utf8SyntaxFormatter.Write(
                body.Document,
                body.SelectionSetCursor,
                _writer,
                variables: default,
                indented: false);
        }
    }

    private readonly void WriteFragmentName(int ordinal, Span<byte> ordinalBuffer)
    {
        _writer.Write("_fusion_body_"u8);
        Utf8Formatter.TryFormat(ordinal, ordinalBuffer, out var written);
        _writer.Write(ordinalBuffer.Slice(0, written));
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

                // The name of the definition is its only variable site, so every site in the
                // definition is renamed and no ordinal has to be excluded.
                Utf8SyntaxFormatter.Write(
                    document,
                    cursor,
                    _writer,
                    prefix,
                    SharedOrdinalSet.Empty);
            }

            cursor += definitionRow.NumberOfRows;
        }
    }

    private void WriteDefinitionSeparator()
    {
        if (_parenOpened)
        {
            _writer.Write(","u8);
        }
        else
        {
            _writer.Write("("u8);
            _parenOpened = true;
        }
    }

    // The delimiter that opens the JSON string value is written with the first content byte, so a
    // builder that throws before it writes anything leaves the destination buffer untouched.
    private void EnsureOpened()
    {
        if (!_opened)
        {
            _writer.WriteDelimiter();
            _opened = true;
        }
    }

    private void EnsureKeyword()
    {
        if (!_keywordWritten)
        {
            EnsureOpened();
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

    /// <summary>
    /// Returns the index of the body of the added selection, or <c>-1</c> when the selection
    /// declares no body. A body is matched by its document and its selection set, and the type
    /// condition of its first item is kept.
    /// </summary>
    private int RegisterBody(
        Utf8OperationDocument document,
        int selectionSetCursor,
        string? typeConditionText,
        ReadOnlyMemory<byte> typeConditionUtf8)
    {
        if (selectionSetCursor < 0)
        {
            return -1;
        }

        for (var i = 0; i < _bodyCount; i++)
        {
            ref var body = ref _bodies![i];

            if (!ReferenceEquals(body.Document, document)
                || body.SelectionSetCursor != selectionSetCursor)
            {
                continue;
            }

            if (!body.HasTypeCondition(typeConditionText, typeConditionUtf8.Span))
            {
                throw new InvalidOperationException(
                    "The items that share a body must declare the same type condition.");
            }

            body.ItemCount++;
            return i;
        }

        if (_bodies is null || _bodies.Length == _bodyCount)
        {
            GrowBodies();
        }

        _bodies![_bodyCount] = new SelectionBody(
            document,
            selectionSetCursor,
            typeConditionText,
            typeConditionUtf8);

        return _bodyCount++;
    }

    /// <summary>
    /// Determines whether every variable the body at <paramref name="selectionSetCursor"/> uses
    /// keeps its original name.
    /// </summary>
    private readonly bool UsesSharedVariablesOnly(
        Utf8OperationDocument document,
        int selectionSetCursor,
        Span<ulong> bits)
    {
        var siteCount = document.VariableSiteCount;

        if (siteCount == 0)
        {
            return true;
        }

        var row = document.GetRow(selectionSetCursor);
        var end = row.SourceEnd;
        var shared = CreateSharedSet(document, bits);

        for (var i = document.FindFirstVariableSite(row.Location); i < siteCount; i++)
        {
            if (document.GetVariableSitePosition(i) >= end)
            {
                break;
            }

            if (!shared.Contains(document.GetVariableSiteOrdinal(i)))
            {
                return false;
            }
        }

        return true;
    }

    private readonly SharedOrdinalSet CreateSharedSet(
        Utf8OperationDocument document,
        Span<ulong> bits)
    {
        var variableCount = document.VariableCount;
        var shared = new SharedOrdinalSet(bits.Slice(0, (variableCount + 63) >> 6));

        for (var ordinal = 0; ordinal < variableCount; ordinal++)
        {
            if (ContainsSharedDefinition(document.GetVariableName(ordinal)))
            {
                shared.Add(ordinal);
            }
        }

        return shared;
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
            if (ReferenceEquals(_items![i].Field.Document, document))
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
            var count = _items![i].Field.Document.VariableCount;
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
        var next = ArrayPool<Item>.Shared.Rent(capacity);

        if (_items is not null)
        {
            Array.Copy(_items, next, _itemCount);
            ArrayPool<Item>.Shared.Return(_items, clearArray: true);
        }

        _items = next;
    }

    private void GrowBodies()
    {
        var capacity = _bodies is null ? InitialCapacity : _bodies.Length * 2;
        var next = ArrayPool<SelectionBody>.Shared.Rent(capacity);

        if (_bodies is not null)
        {
            Array.Copy(_bodies, next, _bodyCount);
            ArrayPool<SelectionBody>.Shared.Return(_bodies, clearArray: true);
        }

        _bodies = next;
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
            ArrayPool<Item>.Shared.Return(_items, clearArray: true);
            _items = null;
        }

        if (_bodies is not null)
        {
            ArrayPool<SelectionBody>.Shared.Return(_bodies, clearArray: true);
            _bodies = null;
        }

        if (_sharedDefinitions is not null)
        {
            ArrayPool<Utf8VariableDefinitionNode>.Shared.Return(_sharedDefinitions, clearArray: true);
            _sharedDefinitions = null;
        }

        _itemCount = 0;
        _bodyCount = 0;
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

    /// <summary>
    /// Holds one added root selection together with the body it selects.
    /// </summary>
    private readonly struct Item
    {
        internal Item(Utf8FieldNode field, int bodyIndex)
        {
            Field = field;
            BodyIndex = bodyIndex;
        }

        /// <summary>
        /// Gets the root field selection of this item.
        /// </summary>
        internal Utf8FieldNode Field { get; }

        /// <summary>
        /// Gets the index of the body this item selects, or <c>-1</c> when the item declares no
        /// body.
        /// </summary>
        internal int BodyIndex { get; }
    }

    /// <summary>
    /// Holds one distinct item body, identified by the document it belongs to and the selection set
    /// that holds it.
    /// </summary>
    private struct SelectionBody
    {
        private readonly string? _typeConditionText;
        private readonly ReadOnlyMemory<byte> _typeConditionUtf8;

        internal SelectionBody(
            Utf8OperationDocument document,
            int selectionSetCursor,
            string? typeConditionText,
            ReadOnlyMemory<byte> typeConditionUtf8)
        {
            Document = document;
            SelectionSetCursor = selectionSetCursor;
            _typeConditionText = typeConditionText;
            _typeConditionUtf8 = typeConditionUtf8;
            ItemCount = 1;
        }

        /// <summary>
        /// Gets the document this body belongs to.
        /// </summary>
        internal Utf8OperationDocument Document { get; }

        /// <summary>
        /// Gets the cursor of the selection set that holds this body.
        /// </summary>
        internal int SelectionSetCursor { get; }

        /// <summary>
        /// Gets or sets the number of items that select this body.
        /// </summary>
        internal int ItemCount { get; set; }

        /// <summary>
        /// Gets or sets the one-based ordinal of the fragment that holds this body, or zero when
        /// the body is written inline.
        /// </summary>
        internal int FragmentOrdinal { get; set; }

        /// <summary>
        /// Determines whether this body is selected on the specified type condition, which is given
        /// either as a string or in its UTF-8 encoded form.
        /// </summary>
        internal readonly bool HasTypeCondition(string? text, ReadOnlySpan<byte> utf8)
        {
            if (_typeConditionText is null)
            {
                return text is null
                    ? _typeConditionUtf8.Span.SequenceEqual(utf8)
                    : EqualsAscii(text, _typeConditionUtf8.Span);
            }

            return text is null
                ? EqualsAscii(_typeConditionText, utf8)
                : string.Equals(_typeConditionText, text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Writes the type condition this body is selected on.
        /// </summary>
        internal readonly void WriteTypeCondition(Utf8SyntaxWriter writer)
        {
            if (_typeConditionText is null)
            {
                writer.Write(_typeConditionUtf8.Span);
                return;
            }

            writer.Write(_typeConditionText);
        }

        // A GraphQL name holds ASCII characters only, so each character is one UTF-8 byte.
        private static bool EqualsAscii(string text, ReadOnlySpan<byte> utf8)
        {
            if (text.Length != utf8.Length)
            {
                return false;
            }

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != utf8[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
