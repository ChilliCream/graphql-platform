using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLParser
    {
        private readonly ParserOptions _options;
        private readonly bool _createLocation;
        private Utf8GraphQLReader _reader;
        private StringValueNode _description;

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
                    "The GraphQL data is empty.",
                    nameof(graphQLData));
            }

            _options = options;
            _createLocation = !options.NoLocations;
            _reader = new Utf8GraphQLReader(graphQLData);
            _description = null;
        }

        private DocumentNode Parse()
        {
            var definitions = new List<IDefinitionNode>();

            TokenInfo start = TokenInfo.FromReader(in _reader);

            MoveNext();

            while (_reader.Kind != TokenKind.EndOfFile)
            {
                definitions.Add(ParseDefinition());
            }

            Location location = CreateLocation(in start);

            return new DocumentNode(location, definitions);
        }

        private IDefinitionNode ParseDefinition()
        {
            if (TokenHelper.IsDescription(in _reader))
            {
                _description = ParseDescription();
            }

            if (reader.Kind == TokenKind.Name)
            {
                if (reader.Value.SequenceEqual(GraphQLKeywords.Query)
                    || reader.Value.SequenceEqual(GraphQLKeywords.Mutation)
                    || reader.Value.SequenceEqual(GraphQLKeywords.Subscription))
                {
                    return ParseOperationDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Fragment))
                {
                    return ParseFragmentDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Schema))
                {
                    ParseSchemaDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Scalar))
                {
                    return ParseScalarTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Type))
                {
                    return ParseObjectTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Interface))
                {
                    return ParseInterfaceTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Union))
                {
                    return ParseUnionTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Enum))
                {
                    return ParseEnumTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Input))
                {
                    return ParseInputObjectTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Extend))
                {
                    return ParseTypeExtension(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Directive))
                {
                    return ParseDirectiveDefinition(context, ref reader);
                }
            }
            else if (reader.Kind == TokenKind.LeftBrace)
            {
                return ParseOperationDefinitionShortHandForm(
                    context, ref reader);
            }

            throw ParserHelper.Unexpected(ref reader, reader.Kind);
        }

        public static Parser Default { get; } = new Parser();
    }
}
