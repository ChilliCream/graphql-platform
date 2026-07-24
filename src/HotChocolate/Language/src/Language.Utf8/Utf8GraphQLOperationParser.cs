using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Buffers;
using static HotChocolate.Language.Properties.LangUtf8Resources;
using DbRow = HotChocolate.Language.Utf8OperationDocument.DbRow;
using MetaDb = HotChocolate.Language.Utf8OperationDocument.MetaDb;
using VariableTable = HotChocolate.Language.Utf8OperationDocument.VariableTable;

namespace HotChocolate.Language;

/// <summary>
/// Parses executable GraphQL documents into a packed, immutable UTF-8 syntax tree.
/// </summary>
public ref struct Utf8GraphQLOperationParser
{
    private readonly ReadOnlySpan<byte> _source;
    private readonly ChunkedArrayWriter _metadata;
    private readonly bool _allowFragmentVariables;
    private readonly int _maxAllowedNodes;
    private readonly int _maxAllowedFields;
    private readonly int _maxAllowedDirectives;
    private readonly int _maxAllowedRecursionDepth;
    private Utf8GraphQLReader _reader;
    private byte[]? _variableSites;
    private byte[]? _variableDirectory;
    private int _siteCount;
    private int _variableCount;
    private int _parsedNodes;
    private int _parsedFields;
    private int _recursionDepth;
    private int _previousEnd;
    private int _openRows;

    private Utf8GraphQLOperationParser(
        ReadOnlySpan<byte> sourceText,
        ChunkedArrayWriter metadata,
        ParserOptions? options)
    {
        options ??= ParserOptions.Default;
        _source = sourceText;
        _metadata = metadata;
        _allowFragmentVariables = options.Experimental.AllowFragmentVariables;
        _maxAllowedNodes = options.MaxAllowedNodes;
        _maxAllowedFields = options.MaxAllowedFields;
        _maxAllowedDirectives = options.MaxAllowedDirectives;
        _maxAllowedRecursionDepth = options.MaxAllowedRecursionDepth;
        _reader = new Utf8GraphQLReader(sourceText, options.MaxAllowedTokens);
        _variableSites = null;
        _variableDirectory = null;
        _siteCount = 0;
        _variableCount = 0;
        _parsedNodes = 0;
        _parsedFields = 0;
        _recursionDepth = 0;
        _previousEnd = 0;
        _openRows = 0;
    }

    private void Parse()
    {
        CountNode();
        MoveNext();

        while (_reader.Kind is not TokenKind.EndOfFile)
        {
            var sourceStart = _reader.Start;

            if (_reader.Kind is TokenKind.String or TokenKind.BlockString)
            {
                CountNode();
                ValidateString();
                MoveNext();
            }

            if (_reader.Kind is TokenKind.LeftBrace)
            {
                ParseOperationDefinition(OperationType.Query, sourceStart, shorthand: true);
                continue;
            }

            if (IsKeyword(GraphQLKeywords.Query))
            {
                ParseOperationDefinition(OperationType.Query, sourceStart, shorthand: false);
                continue;
            }

            if (IsKeyword(GraphQLKeywords.Mutation))
            {
                ParseOperationDefinition(OperationType.Mutation, sourceStart, shorthand: false);
                continue;
            }

            if (IsKeyword(GraphQLKeywords.Subscription))
            {
                ParseOperationDefinition(OperationType.Subscription, sourceStart, shorthand: false);
                continue;
            }

            if (IsKeyword(GraphQLKeywords.Fragment))
            {
                ParseFragmentDefinition(sourceStart);
                continue;
            }

            throw Error("Only executable operation and fragment definitions are supported.");
        }

        if (_openRows != 0)
        {
            throw new InvalidOperationException("The packed syntax metadata is incomplete.");
        }
    }

    private void ParseOperationDefinition(
        OperationType operation,
        int sourceStart,
        bool shorthand)
    {
        var kind = operation switch
        {
            OperationType.Mutation => Utf8SyntaxKind.OperationMutation,
            OperationType.Subscription => Utf8SyntaxKind.OperationSubscription,
            _ => Utf8SyntaxKind.OperationQuery
        };
        var cursor = AppendPlaceholder();

        if (!shorthand)
        {
            MoveNext();

            if (_reader.Kind is TokenKind.Name)
            {
                ReadName(out var nameStart, out var nameLength);
                AppendRow(new DbRow(Utf8SyntaxKind.Name, nameStart, nameLength, 1));
            }
        }

        ParseVariableDefinitions();
        ParseDirectives(isConstant: false);
        var sourceEnd = ParseSelectionSet();

        Patch(cursor, new DbRow(kind, sourceStart, sourceEnd - sourceStart, RowCount - cursor));
    }

    private void ParseFragmentDefinition(int sourceStart)
    {
        var cursor = AppendPlaceholder();
        MoveNext();
        ReadFragmentName(out var nameStart, out var nameLength);
        AppendRow(new DbRow(Utf8SyntaxKind.Name, nameStart, nameLength, 1));

        if (_allowFragmentVariables)
        {
            ParseVariableDefinitions();
        }

        ExpectKeyword(GraphQLKeywords.On);
        // The classic parser counts the type condition as a NamedType node. The removed
        // early type-condition placeholder used to supply that count, so restore it here.
        CountNode();
        ReadName(out var typeStart, out var typeLength);
        AppendRow(new DbRow(Utf8SyntaxKind.TypeCondition, typeStart, typeLength, 1));
        ParseDirectives(isConstant: false);
        var sourceEnd = ParseSelectionSet();

        Patch(
            cursor,
            new DbRow(
                Utf8SyntaxKind.FragmentDefinition,
                sourceStart,
                sourceEnd - sourceStart,
                RowCount - cursor));
    }

    private void ParseVariableDefinitions()
    {
        if (_reader.Kind is not TokenKind.LeftParenthesis)
        {
            return;
        }

        MoveNext();

        while (_reader.Kind is not TokenKind.RightParenthesis)
        {
            if (_reader.Kind is TokenKind.EndOfFile)
            {
                throw Error("Expected a closing parenthesis.");
            }

            var sourceStart = _reader.Start;
            if (_reader.Kind is TokenKind.String or TokenKind.BlockString)
            {
                CountNode();
                ValidateString();
                MoveNext();
            }

            var cursor = AppendPlaceholder();
            CountNode();
            Expect(TokenKind.Dollar);
            ReadName(out var nameStart, out var nameLength);
            AppendRow(new DbRow(Utf8SyntaxKind.Name, nameStart, nameLength, 1));
            RecordVariableSite(nameStart, nameLength);
            Expect(TokenKind.Colon);
            ParseTypeReference();

            if (_reader.Kind is TokenKind.Equal)
            {
                MoveNext();
                ParseValue(isConstant: true);
            }

            ParseDirectives(isConstant: true);
            Patch(
                cursor,
                new DbRow(
                    Utf8SyntaxKind.VariableDefinition,
                    sourceStart,
                    _previousEnd - sourceStart,
                    RowCount - cursor));
        }

        MoveNext();
    }

    private int ParseSelectionSet()
    {
        EnterDepth();
        try
        {
            var sourceStart = _reader.Start;
            Expect(TokenKind.LeftBrace);
            var cursor = AppendPlaceholder();

            while (_reader.Kind is not TokenKind.RightBrace)
            {
                if (_reader.Kind is TokenKind.EndOfFile)
                {
                    throw Error("Expected a closing brace.");
                }

                ParseSelection();
            }

            var sourceEnd = _reader.End;
            MoveNext();
            Patch(
                cursor,
                new DbRow(
                    Utf8SyntaxKind.SelectionSet,
                    sourceStart,
                    sourceEnd - sourceStart,
                    RowCount - cursor));
            return sourceEnd;
        }
        finally
        {
            ExitDepth();
        }
    }

    private void ParseSelection()
    {
        if (_reader.Kind is TokenKind.Spread)
        {
            ParseFragmentSelection();
        }
        else
        {
            ParseField();
        }
    }

    private void ParseField()
    {
        if (++_parsedFields > _maxAllowedFields)
        {
            throw Error($"The maximum allowed number of fields ({_maxAllowedFields}) was exceeded.");
        }

        var sourceStart = _reader.Start;
        var cursor = AppendPlaceholder();
        ReadName(out var firstStart, out var firstLength);

        if (_reader.Kind is TokenKind.Colon)
        {
            // The first name is the alias; in source order the alias token precedes the name.
            AppendRow(new DbRow(Utf8SyntaxKind.Alias, firstStart, firstLength, 1));
            MoveNext();
            ReadName(out var nameStart, out var nameLength);
            AppendRow(new DbRow(Utf8SyntaxKind.Name, nameStart, nameLength, 1));
        }
        else
        {
            AppendRow(new DbRow(Utf8SyntaxKind.Name, firstStart, firstLength, 1));
        }

        ParseArguments(isConstant: false);
        ParseDirectives(isConstant: false);

        var sourceEnd = _previousEnd;
        if (_reader.Kind is TokenKind.LeftBrace)
        {
            sourceEnd = ParseSelectionSet();
        }

        Patch(
            cursor,
            new DbRow(
                Utf8SyntaxKind.Field,
                sourceStart,
                sourceEnd - sourceStart,
                RowCount - cursor));
    }

    private void ParseFragmentSelection()
    {
        var sourceStart = _reader.Start;
        var cursor = AppendPlaceholder();
        MoveNext();

        if (_reader.Kind is TokenKind.Name && !IsKeyword(GraphQLKeywords.On))
        {
            ReadFragmentName(out var nameStart, out var nameLength);
            AppendRow(new DbRow(Utf8SyntaxKind.Name, nameStart, nameLength, 1));
            ParseDirectives(isConstant: false);
            Patch(
                cursor,
                new DbRow(
                    Utf8SyntaxKind.FragmentSpread,
                    sourceStart,
                    _previousEnd - sourceStart,
                    RowCount - cursor));
            return;
        }

        if (IsKeyword(GraphQLKeywords.On))
        {
            MoveNext();
            CountNode();
            ReadName(out var typeStart, out var typeLength);
            AppendRow(new DbRow(Utf8SyntaxKind.TypeCondition, typeStart, typeLength, 1));
        }

        ParseDirectives(isConstant: false);
        var sourceEnd = ParseSelectionSet();
        Patch(
            cursor,
            new DbRow(
                Utf8SyntaxKind.InlineFragment,
                sourceStart,
                sourceEnd - sourceStart,
                RowCount - cursor));
    }

    private void ParseArguments(bool isConstant)
    {
        if (_reader.Kind is not TokenKind.LeftParenthesis)
        {
            return;
        }

        MoveNext();
        while (_reader.Kind is not TokenKind.RightParenthesis)
        {
            CountNode();
            ReadName(out _, out _);
            Expect(TokenKind.Colon);
            ParseValue(isConstant);
        }
        MoveNext();
    }

    private void ParseDirectives(bool isConstant)
    {
        var count = 0;
        while (_reader.Kind is TokenKind.At)
        {
            if (++count > _maxAllowedDirectives)
            {
                throw Error(
                    $"The maximum allowed number of directives ({_maxAllowedDirectives}) was exceeded.");
            }

            CountNode();
            MoveNext();
            ReadName(out _, out _);
            ParseArguments(isConstant);
        }
    }

    private void ParseTypeReference()
    {
        EnterDepth();
        try
        {
            CountNode();
            if (_reader.Kind is TokenKind.LeftBracket)
            {
                MoveNext();
                ParseTypeReference();
                Expect(TokenKind.RightBracket);
            }
            else
            {
                ReadName(out _, out _);
            }

            if (_reader.Kind is TokenKind.Bang)
            {
                CountNode();
                MoveNext();
            }
        }
        finally
        {
            ExitDepth();
        }
    }

    private void ParseValue(bool isConstant)
    {
        EnterDepth();
        CountNode();
        try
        {
            switch (_reader.Kind)
            {
                case TokenKind.Integer:
                case TokenKind.Float:
                case TokenKind.Name:
                    MoveNext();
                    return;

                case TokenKind.String:
                case TokenKind.BlockString:
                    ValidateString();
                    MoveNext();
                    return;

                case TokenKind.Dollar when !isConstant:
                    MoveNext();
                    ReadName(out var nameStart, out var nameLength);
                    RecordVariableSite(nameStart, nameLength);
                    return;

                case TokenKind.LeftBracket:
                    MoveNext();
                    while (_reader.Kind is not TokenKind.RightBracket)
                    {
                        ParseValue(isConstant);
                    }
                    MoveNext();
                    return;

                case TokenKind.LeftBrace:
                    MoveNext();
                    while (_reader.Kind is not TokenKind.RightBrace)
                    {
                        CountNode();
                        ReadName(out _, out _);
                        Expect(TokenKind.Colon);
                        ParseValue(isConstant);
                    }
                    MoveNext();
                    return;

                default:
                    throw Error("Expected a value literal.");
            }
        }
        finally
        {
            ExitDepth();
        }
    }

    private void ReadFragmentName(out int start, out int length)
    {
        if (IsKeyword(GraphQLKeywords.On))
        {
            throw Error("A fragment name cannot be `on`.");
        }

        ReadName(out start, out length);
    }

    private void ReadName(out int start, out int length)
    {
        if (_reader.Kind is not TokenKind.Name)
        {
            throw Error("Expected a name.");
        }

        CountNode();
        start = _reader.Start;
        length = _reader.End - _reader.Start;
        MoveNext();
    }

    private void ExpectKeyword(ReadOnlySpan<byte> keyword)
    {
        if (!IsKeyword(keyword))
        {
            throw Error("Expected a keyword.");
        }

        MoveNext();
    }

    private bool IsKeyword(ReadOnlySpan<byte> keyword)
        => _reader.Kind is TokenKind.Name && _reader.Value.SequenceEqual(keyword);

    private void Expect(TokenKind kind)
    {
        if (_reader.Kind != kind)
        {
            throw Error($"Expected {kind}, found {_reader.Kind}.");
        }

        MoveNext();
    }

    private void MoveNext()
    {
        _previousEnd = _reader.Position;
        _reader.MoveNext();
    }

    private void ValidateString()
        => Utf8Helper.Validate(_reader.Value, _reader.Kind is TokenKind.BlockString);

    private void RecordVariableSite(int nameStart, int nameLength)
    {
        var ordinal = ResolveOrdinal(nameStart, nameLength);

        var required = checked((_siteCount + 1) * 8);
        if (_variableSites is null || _variableSites.Length < required)
        {
            GrowVariableBuffer(ref _variableSites, required);
        }

        var offset = _siteCount * 8;
        ref var start = ref MemoryMarshal.GetReference(_variableSites.AsSpan(offset));
        Unsafe.WriteUnaligned(ref start, nameStart);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref start, 4), (ushort)ordinal);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref start, 6), (ushort)0);
        _siteCount++;
    }

    private int ResolveOrdinal(int nameStart, int nameLength)
    {
        var name = _source.Slice(nameStart, nameLength);

        for (var i = 0; i < _variableCount; i++)
        {
            ref var entry = ref MemoryMarshal.GetReference(_variableDirectory!.AsSpan(i * 8));
            var existingStart = Unsafe.ReadUnaligned<int>(ref entry);
            var existingLength = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref entry, 4));

            if (name.SequenceEqual(_source.Slice(existingStart, existingLength)))
            {
                return i;
            }
        }

        if (_variableCount > ushort.MaxValue)
        {
            throw Error(
                $"The maximum allowed number of distinct variables ({ushort.MaxValue + 1}) was exceeded.");
        }

        var required = checked((_variableCount + 1) * 8);
        if (_variableDirectory is null || _variableDirectory.Length < required)
        {
            GrowVariableBuffer(ref _variableDirectory, required);
        }

        ref var slot = ref MemoryMarshal.GetReference(_variableDirectory!.AsSpan(_variableCount * 8));
        Unsafe.WriteUnaligned(ref slot, nameStart);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref slot, 4), nameLength);
        return _variableCount++;
    }

    private static void GrowVariableBuffer(ref byte[]? buffer, int required)
    {
        var next = ArrayPool<byte>.Shared.Rent(required);

        if (buffer is not null)
        {
            buffer.AsSpan().CopyTo(next);
            ArrayPool<byte>.Shared.Return(buffer);
        }

        buffer = next;
    }

    private readonly VariableTable CreateVariableTable(bool pooled)
    {
        if (_siteCount == 0)
        {
            return VariableTable.Empty;
        }

        var siteBytes = _siteCount * 8;
        var directoryBytes = _variableCount * 8;
        var length = siteBytes + directoryBytes;
        var buffer = pooled
            ? ArrayPool<byte>.Shared.Rent(length)
            : new byte[length];

        _variableSites.AsSpan(0, siteBytes).CopyTo(buffer);
        _variableDirectory.AsSpan(0, directoryBytes).CopyTo(buffer.AsSpan(siteBytes));
        return VariableTable.Create(buffer, _siteCount, _variableCount, pooled);
    }

    private void ReturnVariableBuffers()
    {
        if (_variableSites is not null)
        {
            ArrayPool<byte>.Shared.Return(_variableSites);
            _variableSites = null;
        }

        if (_variableDirectory is not null)
        {
            ArrayPool<byte>.Shared.Return(_variableDirectory);
            _variableDirectory = null;
        }
    }

    private int AppendPlaceholder()
    {
        CountNode();
        var metadataLength = _metadata.Length;
        AssertCanAppendRow(metadataLength);
        var cursor = metadataLength / DbRow.Size;
        _metadata.GetSpan(DbRow.Size);
        _metadata.Advance(DbRow.Size);
        _openRows++;
        return cursor;
    }

    private readonly void AppendRow(in DbRow row)
    {
        AssertCanAppendRow(_metadata.Length);
        var buffer = _metadata.GetSpan(DbRow.Size);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer), row);
        _metadata.Advance(DbRow.Size);
    }

    internal static void AssertCanAppendRow(int metadataLength)
    {
        if ((uint)metadataLength > int.MaxValue - DbRow.Size)
        {
            throw new OverflowException("The packed syntax metadata exceeded its maximum size.");
        }
    }

    internal static int GetNextMetadataLength(int metadataLength)
    {
        AssertCanAppendRow(metadataLength);
        return metadataLength + DbRow.Size;
    }

    private void Patch(int cursor, DbRow row)
    {
        Span<byte> buffer = stackalloc byte[DbRow.Size];
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer), row);
        _metadata.WriteAt(checked(cursor * DbRow.Size), buffer);
        _openRows--;
    }

    private readonly int RowCount => _metadata.Length / DbRow.Size;

    private void CountNode()
    {
        if (++_parsedNodes > _maxAllowedNodes)
        {
            throw Error(
                $"The maximum allowed number of syntax nodes ({_maxAllowedNodes}) was exceeded.");
        }
    }

    private void EnterDepth()
    {
        if (++_recursionDepth > _maxAllowedRecursionDepth)
        {
            throw Error(
                $"The maximum allowed recursion depth ({_maxAllowedRecursionDepth}) was exceeded.");
        }
    }

    private void ExitDepth() => _recursionDepth--;

    private readonly SyntaxException Error(string message) => new(_reader, message);

    /// <summary>
    /// Parses an executable GraphQL document into a packed UTF-8 syntax tree. The returned
    /// document adopts <paramref name="sourceText"/> as its source; the caller must not
    /// mutate the array afterwards. The document requires no disposal.
    /// </summary>
    /// <param name="sourceText">
    /// The UTF-8 encoded GraphQL source text that becomes the document's source.
    /// </param>
    /// <param name="options">
    /// The parser options, or <see langword="null"/> to use the defaults.
    /// </param>
    /// <returns>
    /// The parsed document.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="sourceText"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="sourceText"/> is empty.
    /// </exception>
    public static Utf8OperationDocument Parse(
        byte[] sourceText,
        ParserOptions? options = null)
    {
        if (sourceText is null)
        {
            throw new ArgumentNullException(nameof(sourceText));
        }

        if (sourceText.Length == 0)
        {
            throw new ArgumentException(GraphQLData_Empty, nameof(sourceText));
        }

        return Parse(new ReadOnlyMemorySegment(sourceText), options);
    }

    /// <summary>
    /// Parses an executable GraphQL document into a packed UTF-8 syntax tree. The returned
    /// document is a view over <paramref name="sourceText"/>; the segment's owner must
    /// outlive the document.
    /// </summary>
    /// <param name="sourceText">
    /// The GraphQL source text that the document views.
    /// </param>
    /// <param name="options">
    /// The parser options, or <see langword="null"/> to use the defaults.
    /// </param>
    /// <param name="pooledMetaDb">
    /// When <see langword="true"/>, the packed metadata is stored in a pooled buffer and the
    /// document must be disposed to return it. When <see langword="false"/>, the metadata is
    /// stored in an exactly sized buffer and disposal is optional.
    /// </param>
    /// <returns>
    /// The parsed document.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="sourceText"/> is empty.
    /// </exception>
    public static Utf8OperationDocument Parse(
        ReadOnlyMemorySegment sourceText,
        ParserOptions? options = null,
        bool pooledMetaDb = false)
    {
        if (sourceText.IsEmpty)
        {
            throw new ArgumentException(GraphQLData_Empty, nameof(sourceText));
        }

        using var writer = new ChunkedArrayWriter(JsonMemoryKind.Metadata);
        var parser = new Utf8GraphQLOperationParser(sourceText.Span, writer, options);
        try
        {
            parser.Parse();

            return new Utf8OperationDocument(
                sourceText,
                CreateMetaDb(writer, pooledMetaDb),
                parser.CreateVariableTable(pooledMetaDb));
        }
        finally
        {
            parser.ReturnVariableBuffers();
        }
    }

    private static MetaDb CreateMetaDb(ChunkedArrayWriter writer, bool pooled)
    {
        var length = writer.Length;
        var buffer = pooled
            ? ArrayPool<byte>.Shared.Rent(length)
            : new byte[length];
        writer.CopyTo(buffer, 0, length);
        return MetaDb.Create(buffer, length, pooled);
    }
}
