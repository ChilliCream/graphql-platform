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
                if (TokenHelper.IsSchemaKeyword(in reader))
                {
                    return ParseSchemaExtension(context, in reader);
                }

                switch (reade.Value)
                {
                    case Keywords.Schema:

                    case Keywords.Scalar:
                        return ParseScalarTypeExtension(context);
                    case Keywords.Type:
                        return ParseObjectTypeExtension(context);
                    case Keywords.Interface:
                        return ParseInterfaceTypeExtension(context);
                    case Keywords.Union:
                        return ParseUnionTypeExtension(context);
                    case Keywords.Enum:
                        return ParseEnumTypeExtension(context);
                    case Keywords.Input:
                        return ParseInputObjectTypeExtension(context);
                }
            }

            throw context.Unexpected(keywordToken);
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
            ParserHelper.ExpectSchemaKeyword(in reader);

            List<DirectiveNode> directives =
                ParseDirectives(context, in reader, true);

            List<OperationTypeDefinitionNode> operationTypeDefinitions =
                ParseOperationTypeDefinitions(context, in reader);

            if (directives.Count == 0 && operationTypeDefinitions.Count == 0)
            {
                throw context.Unexpected(start);
            }

            Location location = context.CreateLocation(start);

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
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectScalarKeyword();
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            if (directives.Count == 0)
            {
                throw context.Unexpected(start);
            }
            Location location = context.CreateLocation(start);

            return new ScalarTypeExtensionNode
            (
                location,
                name,
                directives
            );
        }

        private static ObjectTypeExtensionNode ParseObjectTypeExtension(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectTypeKeyword();
            NameNode name = ParseName(context);
            List<NamedTypeNode> interfaces =
                ParseImplementsInterfaces(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            List<FieldDefinitionNode> fields =
                ParseFieldsDefinition(context);
            Location location = context.CreateLocation(start);

            if (interfaces.Count == 0
                && directives.Count == 0
                && fields.Count == 0)
            {
                throw context.Unexpected(start);
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
