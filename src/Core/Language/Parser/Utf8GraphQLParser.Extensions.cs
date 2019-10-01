using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLParser
    {
        private static readonly List<OperationTypeDefinitionNode> _emptyOpDefs =
            new List<OperationTypeDefinitionNode>();

        private ITypeSystemExtensionNode ParseTypeExtension()
        {
            TokenInfo start = Start();

            // extensions do not have a description
            TakeDescription();

            MoveNext();

            if (_reader.Kind == TokenKind.Name)
            {
                if (_reader.Value.SequenceEqual(GraphQLKeywords.Schema))
                {
                    return ParseSchemaExtension(in start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Scalar))
                {
                    return ParseScalarTypeExtension(in start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Type))
                {
                    return ParseObjectTypeExtension(in start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Interface))
                {
                    return ParseInterfaceTypeExtension(in start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Union))
                {
                    return ParseUnionTypeExtension(in start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Enum))
                {
                    return ParseEnumTypeExtension(in start);
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Input))
                {
                    return ParseInputObjectTypeExtension(in start);
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
        private SchemaExtensionNode ParseSchemaExtension(in TokenInfo start)
        {
            MoveNext();

            List<DirectiveNode> directives = ParseDirectives(true);

            List<OperationTypeDefinitionNode> operationTypeDefinitions =
                ParseOperationTypeDefs();

            if (directives.Count == 0 && operationTypeDefinitions.Count == 0)
            {
                throw Unexpected(_reader.Kind);
            }

            Location? location = CreateLocation(in start);

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
            in TokenInfo start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            if (directives.Count == 0)
            {
                throw Unexpected(_reader.Kind);
            }
            Location? location = CreateLocation(in start);

            return new ScalarTypeExtensionNode
            (
                location,
                name,
                directives
            );
        }

        private ObjectTypeExtensionNode ParseObjectTypeExtension(
            in TokenInfo start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<NamedTypeNode> interfaces = ParseImplementsInterfaces();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<FieldDefinitionNode> fields = ParseFieldsDefinition();
            Location? location = CreateLocation(in start);

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
            in TokenInfo start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<FieldDefinitionNode> fields = ParseFieldsDefinition();
            Location? location = CreateLocation(in start);

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
            in TokenInfo start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<NamedTypeNode> types = ParseUnionMemberTypes();
            Location? location = CreateLocation(in start);

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

        private EnumTypeExtensionNode ParseEnumTypeExtension(in TokenInfo start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<EnumValueDefinitionNode> values = ParseEnumValuesDefinition();
            Location? location = CreateLocation(in start);

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
            in TokenInfo start)
        {
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<InputValueDefinitionNode> fields =
                ParseInputFieldsDefinition();
            Location? location = CreateLocation(in start);

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
