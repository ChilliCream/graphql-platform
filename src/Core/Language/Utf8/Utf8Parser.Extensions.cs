using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLParser
    {
        private static ITypeExtensionNode ParseTypeExtension(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            // extensions do not have a description
            context.PopDescription();

            ParserHelper.MoveNext(ref reader);

            if (reader.Kind == TokenKind.Name)
            {
                if (reader.Value.SequenceEqual(GraphQLKeywords.Schema))
                {
                    return ParseSchemaExtension(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Scalar))
                {
                    return ParseScalarTypeExtension(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Type))
                {
                    return ParseObjectTypeExtension(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Interface))
                {
                    return ParseInterfaceTypeExtension(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Union))
                {
                    return ParseUnionTypeExtension(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Enum))
                {
                    return ParseEnumTypeExtension(context, ref reader);
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Input))
                {
                    return ParseInputObjectTypeExtension(context, ref reader);
                }
            }

            throw ParserHelper.Unexpected(ref reader, reader.Kind);
        }

        /// <summary>
        /// Parse schema definition extension.
        /// <see cref="SchemaExtensionNode" />:
        /// * - extend schema Directives[Const]? { OperationTypeDefinition+ }
        /// * - extend schema Directives[Const]
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static SchemaExtensionNode ParseSchemaExtension(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            ParserHelper.MoveNext(ref reader);

            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);

            List<OperationTypeDefinitionNode> operationTypeDefinitions =
                ParseOperationTypeDefinitions(context, ref reader);

            if (directives.Count == 0 && operationTypeDefinitions.Count == 0)
            {
                throw ParserHelper.Unexpected(ref reader, reader.Kind);
            }

            Location location = context.CreateLocation(ref reader);

            return new SchemaExtensionNode
            (
                location,
                directives,
                operationTypeDefinitions
            );
        }

        private static List<OperationTypeDefinitionNode> ParseOperationTypeDefinitions(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.LeftBrace)
            {
                var list = new List<OperationTypeDefinitionNode>();

                // skip opening token
                ParserHelper.MoveNext(ref reader);

                while (reader.Kind != TokenKind.RightBrace)
                {
                    list.Add(ParseOperationTypeDefinition(context, ref reader));
                }

                // skip closing token
                ParserHelper.Expect(ref reader, TokenKind.RightBrace);

                return list;
            }

            return new List<OperationTypeDefinitionNode>();
        }

        private static ScalarTypeExtensionNode ParseScalarTypeExtension(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            if (directives.Count == 0)
            {
                throw ParserHelper.Unexpected(ref reader, reader.Kind);
            }
            Location location = context.CreateLocation(ref reader);

            return new ScalarTypeExtensionNode
            (
                location,
                name,
                directives
            );
        }

        private static ObjectTypeExtensionNode ParseObjectTypeExtension(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            List<NamedTypeNode> interfaces =
                ParseImplementsInterfaces(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            List<FieldDefinitionNode> fields =
                ParseFieldsDefinition(context, ref reader);
            Location location = context.CreateLocation(ref reader);

            if (interfaces.Count == 0
                && directives.Count == 0
                && fields.Count == 0)
            {
                throw ParserHelper.Unexpected(ref reader, reader.Kind);
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

        private static InterfaceTypeExtensionNode ParseInterfaceTypeExtension(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            List<FieldDefinitionNode> fields =
                ParseFieldsDefinition(context, ref reader);
            Location location = context.CreateLocation(ref reader);

            if (directives.Count == 0
                && fields.Count == 0)
            {
                throw ParserHelper.Unexpected(ref reader, reader.Kind);
            }

            return new InterfaceTypeExtensionNode
            (
                location,
                name,
                directives,
                fields
            );
        }

        private static UnionTypeExtensionNode ParseUnionTypeExtension(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            List<NamedTypeNode> types =
                ParseUnionMemberTypes(context, ref reader);
            Location location = context.CreateLocation(ref reader);

            if (directives.Count == 0 && types.Count == 0)
            {
                throw ParserHelper.Unexpected(ref reader, reader.Kind);
            }

            return new UnionTypeExtensionNode
            (
                location,
                name,
                directives,
                types
            );
        }

        private static EnumTypeExtensionNode ParseEnumTypeExtension(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            List<EnumValueDefinitionNode> values =
                ParseEnumValuesDefinition(context, ref reader);
            Location location = context.CreateLocation(ref reader);

            if (directives.Count == 0 && values.Count == 0)
            {
                throw ParserHelper.Unexpected(ref reader, reader.Kind);
            }

            return new EnumTypeExtensionNode
            (
                location,
                name,
                directives,
                values
            );
        }

        private static InputObjectTypeExtensionNode ParseInputObjectTypeExtension(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            List<InputValueDefinitionNode> fields =
                ParseInputFieldsDefinition(context, ref reader);
            Location location = context.CreateLocation(ref reader);

            if (directives.Count == 0 && fields.Count == 0)
            {
                throw ParserHelper.Unexpected(ref reader, reader.Kind);
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
