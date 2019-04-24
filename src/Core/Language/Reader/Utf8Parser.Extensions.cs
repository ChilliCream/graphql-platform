using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public partial class Utf8Parser
    {
        private static ITypeExtensionNode ParseTypeExtension(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);

            ParserHelper.ExpectExtendKeyword(in reader);

            if (reader.Kind == TokenKind.Name)
            {
                if (reader.Value.SequenceEqual(Utf8Keywords.Schema))
                {
                    return ParseSchemaExtension(context, in reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Scalar))
                {
                    return ParseScalarTypeExtension(context, in reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Type))
                {
                    return ParseObjectTypeExtension(context, in reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Interface))
                {
                    return ParseInterfaceTypeExtension(context, in reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Union))
                {
                    return ParseUnionTypeExtension(context, in reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Enum))
                {
                    return ParseEnumTypeExtension(context, in reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Input))
                {
                    return ParseInputObjectTypeExtension(context, in reader);
                }
            }

            throw ParserHelper.Unexpected(in reader, reader.Kind);
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
            in Utf8GraphQLReader reader)
        {
            reader.Read();

            List<DirectiveNode> directives =
                ParseDirectives(context, in reader, true);

            List<OperationTypeDefinitionNode> operationTypeDefinitions =
                ParseOperationTypeDefinitions(context, in reader);

            if (directives.Count == 0 && operationTypeDefinitions.Count == 0)
            {
                throw ParserHelper.Unexpected(in reader, reader.Kind);
            }

            Location location = context.CreateLocation(in reader);

            return new SchemaExtensionNode
            (
                location,
                directives,
                operationTypeDefinitions
            );
        }

        private static List<OperationTypeDefinitionNode> ParseOperationTypeDefinitions(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.LeftBrace)
            {
                var list = new List<OperationTypeDefinitionNode>();

                // skip opening token
                reader.Read();

                while (reader.Kind != TokenKind.RightBrace)
                {
                    list.Add(ParseOperationTypeDefinition(context, in reader));
                }

                // skip closing token
                ParserHelper.Expect(in reader, TokenKind.RightBrace);

                return list;
            }

            return new List<OperationTypeDefinitionNode>();
        }

        private static ScalarTypeExtensionNode ParseScalarTypeExtension(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            reader.Read();

            NameNode name = ParseName(context, in reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, in reader, true);
            if (directives.Count == 0)
            {
                throw ParserHelper.Unexpected(in reader, reader.Kind);
            }
            Location location = context.CreateLocation(in reader);

            return new ScalarTypeExtensionNode
            (
                location,
                name,
                directives
            );
        }

        private static ObjectTypeExtensionNode ParseObjectTypeExtension(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            NameNode name = ParseName(context, in reader);
            List<NamedTypeNode> interfaces =
                ParseImplementsInterfaces(context, in reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, in reader, true);
            List<FieldDefinitionNode> fields =
                ParseFieldsDefinition(context, in reader);
            Location location = context.CreateLocation(in reader);

            if (interfaces.Count == 0
                && directives.Count == 0
                && fields.Count == 0)
            {
                throw ParserHelper.Unexpected(in reader, reader.Kind);
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
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectInterfaceKeyword();
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            List<FieldDefinitionNode> fields =
                ParseFieldsDefinition(context);
            Location location = context.CreateLocation(start);

            if (directives.Count == 0
                && fields.Count == 0)
            {
                throw context.Unexpected(start);
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
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectUnionKeyword();
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            List<NamedTypeNode> types =
                ParseUnionMemberTypes(context);
            Location location = context.CreateLocation(start);

            if (directives.Count == 0 && types.Count == 0)
            {
                throw context.Unexpected(start);
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
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectEnumKeyword();
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            List<EnumValueDefinitionNode> values =
                ParseEnumValuesDefinition(context);
            Location location = context.CreateLocation(start);

            if (directives.Count == 0 && values.Count == 0)
            {
                throw context.Unexpected(start);
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
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectInputKeyword();
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            List<InputValueDefinitionNode> fields =
                ParseInputFieldsDefinition(context);
            Location location = context.CreateLocation(start);

            if (directives.Count == 0 && fields.Count == 0)
            {
                throw context.Unexpected(start);
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
