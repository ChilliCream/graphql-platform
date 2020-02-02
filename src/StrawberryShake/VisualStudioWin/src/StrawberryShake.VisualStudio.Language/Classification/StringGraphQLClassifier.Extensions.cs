using System;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct StringGraphQLClassifier
    {
        private void ParseTypeExtension()
        {
            _classifications.AddClassification(
                SyntaxClassificationKind.ExtendKeyword,
                _reader.Token);
            MoveNext();

            if (_reader.Kind == TokenKind.Name)
            {
                if (_reader.Value.SequenceEqual(GraphQLKeywords.Schema))
                {
                    ParseSchemaExtension();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Scalar))
                {
                    ParseScalarTypeExtension();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Type))
                {
                    ParseObjectTypeExtension();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Interface))
                {
                    ParseInterfaceTypeExtension();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Union))
                {
                    ParseUnionTypeExtension();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Enum))
                {
                    ParseEnumTypeExtension();
                }
                else if (_reader.Value.SequenceEqual(GraphQLKeywords.Input))
                {
                    ParseInputObjectTypeExtension();
                }
                else
                {
                    _classifications.AddClassification(
                        SyntaxClassificationKind.Error,
                        _reader.Token);
                }
            }
            else
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
            }
        }

        /// <summary>
        /// Parse schema definition extension.
        /// <see cref="SchemaExtensionNode" />:
        /// - extend schema Directives[Const]? { OperationTypeDefinition+ }
        /// - extend schema Directives[Const]
        /// </summary>
        private void ParseSchemaExtension() => ParseSchemaDefinition();

        private void ParseScalarTypeExtension() => ParseScalarTypeDefinition();

        private void ParseObjectTypeExtension() => ParseObjectTypeDefinition();

        private void ParseInterfaceTypeExtension() => ParseInterfaceTypeDefinition();

        private void ParseUnionTypeExtension() => ParseUnionTypeDefinition();

        private void ParseEnumTypeExtension() => ParseEnumTypeDefinition();

        private void ParseInputObjectTypeExtension() => ParseInputObjectTypeDefinition();
    }
}
