using System;
using System.Buffers;
using System.Collections.Generic;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLParser
    {
        private readonly ParserOptions _options;
        private readonly bool _createLocation;
        private readonly bool _allowFragmentVars;
        private Utf8GraphQLReader _reader;
        private StringValueNode? _description;

        public Utf8GraphQLParser(
            ReadOnlySpan<byte> graphQLData)
            : this(graphQLData, ParserOptions.Default)
        {
        }

        public Utf8GraphQLParser(
            ReadOnlySpan<byte> graphQLData,
            ParserOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (graphQLData.Length == 0)
            {
                throw new ArgumentException(
                    LangResources.GraphQLData_Empty,
                    nameof(graphQLData));
            }

            _options = options;
            _createLocation = !options.NoLocations;
            _allowFragmentVars = options.Experimental.AllowFragmentVariables;
            _reader = new Utf8GraphQLReader(graphQLData);
            _description = null;
        }

        internal Utf8GraphQLParser(
            Utf8GraphQLReader reader,
            ParserOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (reader.Kind == TokenKind.EndOfFile)
            {
                throw new ArgumentException(
                    LangResources.GraphQLData_Empty,
                    nameof(reader));
            }

            _options = options;
            _createLocation = !options.NoLocations;
            _allowFragmentVars = options.Experimental.AllowFragmentVariables;
            _reader = reader;
            _description = null;
        }

        public DocumentNode Parse()
        {
            var definitions = new List<IDefinitionNode>();

            TokenInfo start = Start();

            MoveNext();

            while (_reader.Kind != TokenKind.EndOfFile)
            {
                definitions.Add(ParseDefinition());
            }

            Location? location = CreateLocation(in start);

            return new DocumentNode(location, definitions);
        }

        internal IReadOnlyList<ArgumentNode> ParseArguments()
        {
            MoveNext();
            return ParseArguments(false);
        }

        private IDefinitionNode ParseDefinition()
        {
            _description = null;
            if (TokenHelper.IsDescription(in _reader))
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
            ReadOnlySpan<byte> graphQLData) =>
            new Utf8GraphQLParser(graphQLData).Parse();

        public static DocumentNode Parse(
            ReadOnlySpan<byte> graphQLData,
            ParserOptions options) =>
            new Utf8GraphQLParser(graphQLData, options).Parse();

        public static DocumentNode Parse(string sourceText) =>
            Parse(sourceText, ParserOptions.Default);

        public static unsafe DocumentNode Parse(
            string sourceText,
            ParserOptions options)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentException(
                    LangResources.SourceText_Empty,
                    nameof(sourceText));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            int length = checked(sourceText.Length * 4);
            bool useStackalloc =
                length <= GraphQLConstants.StackallocThreshold;

            byte[]? source = null;

            Span<byte> sourceSpan = useStackalloc
                ? stackalloc byte[length]
                : (source = ArrayPool<byte>.Shared.Rent(length));

            try
            {
                ConvertToBytes(sourceText, ref sourceSpan);
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

        internal unsafe static int ConvertToBytes(
            string text,
            ref Span<byte> buffer)
        {
            fixed (byte* bytePtr = buffer)
            {
                fixed (char* stringPtr = text)
                {
                    int length = StringHelper.UTF8Encoding.GetBytes(
                        stringPtr, text.Length,
                        bytePtr, buffer.Length);
                    buffer = buffer.Slice(0, length);
                    return length;
                }
            }
        }
    }
}
