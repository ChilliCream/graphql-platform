using System;
using System.Collections.Generic;
using StrawberryShake.VisualStudio.Language.Properties;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct StringGraphQLClassifier
    {
        private readonly ICollection<SyntaxClassification> _classifications;
        private StringGraphQLReader _reader;

        public StringGraphQLClassifier(
            ReadOnlySpan<char> graphQLData,
            ICollection<SyntaxClassification> classifications)
        {
            if (graphQLData.Length == 0)
            {
                throw new ArgumentException(
                    LangResources.GraphQLData_Empty,
                    nameof(graphQLData));
            }

            if (classifications is null)
            {
                throw new ArgumentNullException(nameof(classifications));
            }

            _classifications = classifications;
            _reader = new StringGraphQLReader(graphQLData);
        }

        internal StringGraphQLClassifier(
            StringGraphQLReader reader,
            ICollection<SyntaxClassification> classifications)
        {
            if (reader.Kind == TokenKind.EndOfFile)
            {
                throw new ArgumentException(
                    LangResources.GraphQLData_Empty,
                    nameof(reader));
            }

            if (classifications is null)
            {
                throw new ArgumentNullException(nameof(classifications));
            }

            _classifications = classifications;
            _reader = reader;
        }


        public void Parse()
        {
            MoveNext();

            while (_reader.Kind != TokenKind.EndOfFile)
            {
                ParseDefinition();
            }
        }

        private void ParseDefinition()
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
                    ParseOperationDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Fragment))
                {
                    ParseFragmentDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Schema))
                {
                    ParseSchemaDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Scalar))
                {
                    ParseScalarTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Type))
                {
                    ParseObjectTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Interface))
                {
                    ParseInterfaceTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Union))
                {
                    ParseUnionTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Enum))
                {
                    ParseEnumTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Input))
                {
                    ParseInputObjectTypeDefinition();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Extend))
                {
                    ParseTypeExtension();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Directive))
                {
                    ParseDirectiveDefinition();
                }
                else
                {
                    _classifications.AddClassification(
                        SyntaxClassificationKind.Error,
                        _reader.Token);
                    MoveNext();
                }
            }
            else if (_reader.Kind == TokenKind.LeftBrace)
            {
                ParseShortOperationDefinition();
            }
            else
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
                 MoveNext();
            }
        }
    }
}
