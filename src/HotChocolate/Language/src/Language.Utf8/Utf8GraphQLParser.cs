using System.Buffers;
using System.Diagnostics.CodeAnalysis;
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

    public Utf8GraphQLParser(
        ReadOnlySpan<byte> graphQLData,
        ParserOptions? options = null)
    {
        if (graphQLData.Length == 0)
        {
            throw new ArgumentException(GraphQLData_Empty, nameof(graphQLData));
        }

        options ??= ParserOptions.Default;
        _createLocation = !options.NoLocations;
        _allowFragmentVars = options.Experimental.AllowFragmentVariables;
        _maxAllowedNodes = options.MaxAllowedNodes;
        _maxAllowedFields = options.MaxAllowedFields;
        _reader = new Utf8GraphQLReader(graphQLData, options.MaxAllowedTokens);
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
        _parsedNodes = 0;
        var definitions = new List<IDefinitionNode>();

        var start = Start();

        MoveNext();

        while (_reader.Kind != TokenKind.EndOfFile)
        {
            definitions.Add(ParseDefinition());
        }

        var location = CreateLocation(in start);

        return new DocumentNode(location, definitions, _parsedNodes, _parsedFields);
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
            if (_reader.Value.SequenceEqual(GraphQLKeywords.Query)
                || _reader.Value.SequenceEqual(GraphQLKeywords.Mutation)
                || _reader.Value.SequenceEqual(GraphQLKeywords.Subscription))
            {
                return ParseOperationDefinition();
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Fragment))
            {
                return ParseFragmentDefinition();
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Schema))
            {
                return ParseSchemaDefinition();
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Scalar))
            {
                return ParseScalarTypeDefinition();
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Type))
            {
                return ParseObjectTypeDefinition();
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Interface))
            {
                return ParseInterfaceTypeDefinition();
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Union))
            {
                return ParseUnionTypeDefinition();
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Enum))
            {
                return ParseEnumTypeDefinition();
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Input))
            {
                return ParseInputObjectTypeDefinition();
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Extend))
            {
                return ParseTypeExtension();
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Directive))
            {
                return ParseDirectiveDefinition();
            }
        }
        else if (_reader.Kind == TokenKind.LeftBrace)
        {
            return ParseShortOperationDefinition();
        }

        throw Unexpected(_reader.Kind);
    }

    public static DocumentNode Parse(
        ReadOnlySpan<byte> graphQLData)
    {
        if (graphQLData.Length == 0)
        {
            return new DocumentNode(Array.Empty<IDefinitionNode>());
        }

        return new Utf8GraphQLParser(graphQLData).Parse();
    }

    public static DocumentNode Parse(
        ReadOnlySpan<byte> graphQLData,
        ParserOptions options)
    {
        if (graphQLData.Length == 0)
        {
            return new DocumentNode(Array.Empty<IDefinitionNode>());
        }

        return new Utf8GraphQLParser(graphQLData, options).Parse();
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

        var sourceSpan = length <= GraphQLConstants.StackallocThreshold
            ? stackalloc byte[length]
            : source = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            ConvertToBytes(sourceText, ref sourceSpan);

            if (sourceSpan.Length == 0)
            {
                return new DocumentNode(Array.Empty<IDefinitionNode>());
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
