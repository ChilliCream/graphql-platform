using System;
using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct StringGraphQLParser
    {
        private static readonly List<OperationTypeDefinitionNode> _emptyOpDefs =
            new List<OperationTypeDefinitionNode>();

        private ITypeSystemExtensionNode ParseTypeExtension()
        {
            ISyntaxToken start = _reader.Token;

            // extensions do not have a description
            TakeDescription();

            MoveNext();

            if (_reader.Kind == TokenKind.Name)
            {
                if (_reader.Value.SequenceEqual(GraphQLKeywords.Schema))
                {
                    return ParseSchemaExtension(start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Scalar))
                {
                    return ParseScalarTypeExtension(start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Type))
                {
                    return ParseObjectTypeExtension(start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Interface))
                {
                    return ParseInterfaceTypeExtension(start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Union))
                {
                    return ParseUnionTypeExtension(start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Enum))
                {
                    return ParseEnumTypeExtension(start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Input))
                {
                    return ParseInputObjectTypeExtension(start);
                }
            }

            throw Unexpected(_reader.Kind);
        }

        /// <summary>
        /// Parse schema definition extension.
        /// <see cref="SchemaExtensionNode" />:
        /// * - extend schema Directives[Const]? { OperationTypeDefinition+ }
        /// * - extend schema Directives[Const]
        /// </summary>
        /// <param name="context">The parser context.</param>
        private SchemaExtensionNode ParseSchemaExtension(ISyntaxToken start)
        {
            MoveNext();

            List<DirectiveNode> directives = ParseDirectives(true);

            List<OperationTypeDefinitionNode> operationTypeDefinitions =
                ParseOperationTypeDefs();

            if (directives.Count == 0 && operationTypeDefinitions.Count == 0)
            {
                throw Unexpected(_reader.Kind);
            }

            var location = new Location(start, _reader.Token);

            return new SchemaExtensionNode
            (
                location,
                directives,
                operationTypeDefinitions
            );
        }

        private List<OperationTypeDefinitionNode> ParseOperationTypeDefs()
        {
            if (_reader.Kind == TokenKind.LeftBrace)
            {
                var list = new List<OperationTypeDefinitionNode>();

                // skip opening token
                MoveNext();

                while (_reader.Kind != TokenKind.RightBrace)
                {
                    list.Add(ParseOperationTypeDefinition());
                }

                // skip closing token
                ExpectRightBrace();

                return list;
            }

            return _emptyOpDefs;
        }

        private ScalarTypeExtensionNode ParseScalarTypeExtension(
            ISyntaxToken start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            if (directives.Count == 0)
            {
                throw Unexpected(_reader.Kind);
            }
            var location = new Location(start, _reader.Token);

            return new ScalarTypeExtensionNode
            (
                location,
                name,
                directives
            );
        }

        private ObjectTypeExtensionNode ParseObjectTypeExtension(
            ISyntaxToken start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<NamedTypeNode> interfaces = ParseImplementsInterfaces();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<FieldDefinitionNode> fields = ParseFieldsDefinition();
            var location = new Location(start, _reader.Token);

            if (interfaces.Count == 0
                && directives.Count == 0
                && fields.Count == 0)
            {
                throw Unexpected(_reader.Kind);
            }

            return new ObjectTypeExtensionNode
            (
                location,
                name,
                directives,
                interfaces,
                fields
            );
        }

        private InterfaceTypeExtensionNode ParseInterfaceTypeExtension(
            ISyntaxToken start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<FieldDefinitionNode> fields = ParseFieldsDefinition();
            var location = new Location(start, _reader.Token);

            if (directives.Count == 0
                && fields.Count == 0)
            {
                throw Unexpected(_reader.Kind);
            }

            return new InterfaceTypeExtensionNode
            (
                location,
                name,
                directives,
                fields
            );
        }

        private UnionTypeExtensionNode ParseUnionTypeExtension(
            ISyntaxToken start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<NamedTypeNode> types = ParseUnionMemberTypes();
            var location = new Location(start, _reader.Token);

            if (directives.Count == 0 && types.Count == 0)
            {
                throw Unexpected(_reader.Kind);
            }

            return new UnionTypeExtensionNode
            (
                location,
                name,
                directives,
                types
            );
        }

        private EnumTypeExtensionNode ParseEnumTypeExtension(ISyntaxToken start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<EnumValueDefinitionNode> values = ParseEnumValuesDefinition();
            var location = new Location(start, _reader.Token);

            if (directives.Count == 0 && values.Count == 0)
            {
                throw Unexpected(_reader.Kind);
            }

            return new EnumTypeExtensionNode
            (
                location,
                name,
                directives,
                values
            );
        }

        private InputObjectTypeExtensionNode ParseInputObjectTypeExtension(
            ISyntaxToken start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<InputValueDefinitionNode> fields =
                ParseInputFieldsDefinition();
            var location = new Location(start, _reader.Token);

            if (directives.Count == 0 && fields.Count == 0)
            {
                throw Unexpected(_reader.Kind);
            }

            return new InputObjectTypeExtensionNode
            (
                location,
                name,
                directives,
                fields
            );
        }
    }
}
