using System;
using System.Collections.Generic;
using StrawberryShake.VisualStudio.Language.Properties;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct StringGraphQLClassifier
    {
        private readonly List<SyntaxClassification> classifications;
        private StringGraphQLReader _reader;
        private StringValueNode? _description;

        public StringGraphQLClassifier(ReadOnlySpan<char> graphQLData)
        {
            if (graphQLData.Length == 0)
            {
                throw new ArgumentException(
                    LangResources.GraphQLData_Empty,
                    nameof(graphQLData));
            }

            _classifications = new List<SyntaxClassification>();
            _reader = new StringGraphQLReader(graphQLData);
            _description = null;
        }

        internal StringGraphQLClassifier(StringGraphQLReader reader)
        {
            if (reader.Kind == TokenKind.EndOfFile)
            {
                throw new ArgumentException(
                    LangResources.GraphQLData_Empty,
                    nameof(reader));
            }

            _classifications = new List<SyntaxClassification>();
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

        private IReadOnlyList<ArgumentNode> ParseArguments()
        {
            MoveNext();
            return ParseArguments(false);
        }

        private void ParseDefinition(
            ICollection<SyntaxClassification> classifications)
        {
            if (_isString[(int)_reader.Kind])
            {
                ParseDescription();
            }
            else if (_reader.Kind == TokenKind.Name)
            {
                if (_reader.Value.SequenceEqual(GraphQLKeywords.Query)
                    || _reader.Value.SequenceEqual(GraphQLKeywords.Mutation)
                    || _reader.Value.SequenceEqual(GraphQLKeywords.Subscription))
                {
                    return ParseOperationDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Fragment))
                {
                    return ParseFragmentDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Schema))
                {
                    return ParseSchemaDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Scalar))
                {
                    return ParseScalarTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Type))
                {
                    return ParseObjectTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Interface))
                {
                    return ParseInterfaceTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Union))
                {
                    return ParseUnionTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Enum))
                {
                    return ParseEnumTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Input))
                {
                    return ParseInputObjectTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Extend))
                {
                    return ParseTypeExtension();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Directive))
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
