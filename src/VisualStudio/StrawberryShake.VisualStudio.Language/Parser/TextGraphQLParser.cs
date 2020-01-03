using System;
using System.Collections.Generic;
using StrawberryShake.Language.Properties;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct TextGraphQLParser
    {
        private TextGraphQLReader _reader;
        private StringValueNode? _description;

        public TextGraphQLParser(ReadOnlySpan<char> graphQLData)
        {
            if (graphQLData.Length == 0)
            {
                throw new ArgumentException(
                    LangResources.GraphQLData_Empty,
                    nameof(graphQLData));
            }

            _reader = new TextGraphQLReader(graphQLData);
            _description = null;
        }

        internal TextGraphQLParser(TextGraphQLReader reader)
        {
            if (reader.Kind == TokenKind.EndOfFile)
            {
                throw new ArgumentException(
                    LangResources.GraphQLData_Empty,
                    nameof(reader));
            }

            _reader = reader;
            _description = null;
        }

        public DocumentNode Parse()
        {
            var definitions = new List<IDefinitionNode>();

            ISyntaxToken start = _reader.Token;

            MoveNext();

            while (_reader.Kind != TokenKind.EndOfFile)
            {
                definitions.Add(ParseDefinition());
            }

            var location = new Location(start, _reader.Token);

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
    }
}
