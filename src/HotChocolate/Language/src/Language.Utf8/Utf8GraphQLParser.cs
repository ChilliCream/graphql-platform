using System.Buffers;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLParser
{
    private readonly bool _createLocation;
    private readonly bool _allowFragmentVars;
    private readonly int _maxAllowedNodes;
    private readonly int _maxAllowedFields;
    private Utf8GraphQLReader _reader;
    private StringValueNode? _description;
    private int _parsedNodes;
    private int _parsedFields;
    private Utf8MemoryBuilder? _memory;

    public Utf8GraphQLParser(
        ReadOnlySpan<byte> sourceText,
        ParserOptions? options = null)
    {
        if (sourceText.Length == 0)
        {
            throw new ArgumentException(GraphQLData_Empty, nameof(sourceText));
        }

        options ??= ParserOptions.Default;
        _createLocation = !options.NoLocations;
        _allowFragmentVars = options.Experimental.AllowFragmentVariables;
        _maxAllowedNodes = options.MaxAllowedNodes;
        _maxAllowedFields = options.MaxAllowedFields;
        _reader = new Utf8GraphQLReader(sourceText, options.MaxAllowedTokens);
        _description = null;
    }

    public Utf8GraphQLParser(
        ReadOnlySequence<byte> sourceText,
        ParserOptions? options = null)
    {
        if (sourceText.Length == 0)
        {
            throw new ArgumentException(GraphQLData_Empty, nameof(sourceText));
        }

        options ??= ParserOptions.Default;
        _createLocation = !options.NoLocations;
        _allowFragmentVars = options.Experimental.AllowFragmentVariables;
        _maxAllowedNodes = options.MaxAllowedNodes;
        _maxAllowedFields = options.MaxAllowedFields;
        _reader = new Utf8GraphQLReader(sourceText, options.MaxAllowedTokens);
        _description = null;
    }

    internal Utf8GraphQLParser(
        Utf8GraphQLReader reader,
        ParserOptions? options = null)
    {
        if (reader.Kind == TokenKind.EndOfFile)
        {
            throw new ArgumentException(GraphQLData_Empty, nameof(reader));
        }

        options ??= ParserOptions.Default;
        _createLocation = !options.NoLocations;
        _allowFragmentVars = options.Experimental.AllowFragmentVariables;
        _maxAllowedNodes = options.MaxAllowedNodes;
        _maxAllowedFields = options.MaxAllowedFields;
        _reader = reader;
        _description = null;
    }

    /// <summary>
    /// Gets the number of parsed syntax nodes.
    /// </summary>
    public int ParsedSyntaxNodes => _parsedNodes;

    /// <summary>
    /// Defines if the parser reached the end of the source text.
    /// </summary>
    public bool IsEndOfFile => _reader.Kind is TokenKind.EndOfFile;

    public DocumentNode Parse()
    {
        try
        {
            _parsedNodes = 0;
            var definitions = new List<IDefinitionNode>(16);

            var start = Start();

            MoveNext();

            while (_reader.Kind != TokenKind.EndOfFile)
            {
                definitions.Add(ParseDefinition());
            }

            var location = CreateLocation(in start);

            return new DocumentNode(location, definitions, _parsedNodes, _parsedFields);
        }
        catch
        {
            _memory?.Abandon();
            _memory = null;
            throw;
        }
        finally
        {
            _memory?.Seal();
            _memory = null;
        }
    }

    private IDefinitionNode ParseDefinition()
    {
        _description = null;
        if (TokenHelper.IsDescription(ref _reader))
        {
            _description = ParseDescription();
        }

        if (_reader.Kind == TokenKind.Name)
        {
            if (_reader.Value.Length > 0)
            {
                switch (_reader.Value[0])
                {
                    case (byte)'q':
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Query))
                        {
                            return ParseOperationDefinition(OperationType.Query);
                        }
                        break;

                    case (byte)'m':
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Mutation))
                        {
                            return ParseOperationDefinition(OperationType.Mutation);
                        }
                        break;

                    case (byte)'s':
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Subscription))
                        {
                            return ParseOperationDefinition(OperationType.Subscription);
                        }
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Schema))
                        {
                            return ParseSchemaDefinition();
                        }
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Scalar))
                        {
                            return ParseScalarTypeDefinition();
                        }
                        break;

                    case (byte)'f':
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Fragment))
                        {
                            return ParseFragmentDefinition();
                        }
                        break;

                    case (byte)'t':
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Type))
                        {
                            return ParseObjectTypeDefinition();
                        }
                        break;

                    case (byte)'i':
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Interface))
                        {
                            return ParseInterfaceTypeDefinition();
                        }
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Input))
                        {
                            return ParseInputObjectTypeDefinition();
                        }
                        break;

                    case (byte)'u':
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Union))
                        {
                            return ParseUnionTypeDefinition();
                        }
                        break;

                    case (byte)'e':
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Enum))
                        {
                            return ParseEnumTypeDefinition();
                        }
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Extend))
                        {
                            return ParseTypeExtension();
                        }
                        break;

                    case (byte)'d':
                        if (_reader.Value.SequenceEqual(GraphQLKeywords.Directive))
                        {
                            return ParseDirectiveDefinition();
                        }
                        break;
                }
            }
        }
        else if (_reader.Kind == TokenKind.LeftBrace)
        {
            return ParseShortOperationDefinition();
        }

        throw Unexpected(_reader.Kind);
    }

    public static DocumentNode Parse(
        ReadOnlySpan<byte> sourceText)
    {
        if (sourceText.Length == 0)
        {
            return new DocumentNode([]);
        }

        return new Utf8GraphQLParser(sourceText).Parse();
    }

    public static DocumentNode Parse(
        ReadOnlySpan<byte> sourceText,
        ParserOptions options)
    {
        if (sourceText.Length == 0)
        {
            return new DocumentNode([]);
        }

        return new Utf8GraphQLParser(sourceText, options).Parse();
    }

    public static DocumentNode Parse(
        ReadOnlySequence<byte> sourceText)
    {
        if (sourceText.Length == 0)
        {
            return new DocumentNode([]);
        }

        var parser = new Utf8GraphQLParser(sourceText);
        try
        {
            return parser.Parse();
        }
        finally
        {
            parser._reader.Dispose();
        }
    }

    public static DocumentNode Parse(
        ReadOnlySequence<byte> sourceText,
        ParserOptions options)
    {
        if (sourceText.Length == 0)
        {
            return new DocumentNode([]);
        }

        var parser = new Utf8GraphQLParser(sourceText, options);
        try
        {
            return parser.Parse();
        }
        finally
        {
            parser._reader.Dispose();
        }
    }

    public static DocumentNode Parse(
#if NETSTANDARD2_0
        string sourceText) =>
#else
        [StringSyntax("graphql")] string sourceText) =>
#endif
        Parse(sourceText, ParserOptions.Default);

    public static DocumentNode Parse(
#if NETSTANDARD2_0
        string sourceText,
#else
        [StringSyntax("graphql")] string sourceText,
#endif
        ParserOptions options)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            throw new ArgumentException(SourceText_Empty, nameof(sourceText));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var length = checked(sourceText.Length * 4);
        byte[]? source = null;

        var sourceSpan = length <= GraphQLCharacters.StackallocThreshold
            ? stackalloc byte[length]
            : source = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            ConvertToBytes(sourceText, ref sourceSpan);

            if (sourceSpan.Length == 0)
            {
                return new DocumentNode([]);
            }

            var parser = new Utf8GraphQLParser(sourceSpan, options);
            return parser.Parse();
        }
        finally
        {
            if (source != null)
            {
                sourceSpan.Clear();
                ArrayPool<byte>.Shared.Return(source);
            }
        }
    }

    internal static unsafe void ConvertToBytes(
        string text,
        ref Span<byte> buffer)
    {
        fixed (byte* bytePtr = buffer)
        {
            fixed (char* stringPtr = text)
            {
                var length = StringHelper.UTF8Encoding.GetBytes(
                    stringPtr, text.Length,
                    bytePtr, buffer.Length);
                buffer = buffer.Slice(0, length);
            }
        }
    }
}
