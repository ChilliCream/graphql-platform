using System.Collections.Generic;

namespace HotChocolate.Language
{
    public partial class Parser
    {
        private static ITypeExtensionNode ParseTypeExtension(
            ParserContext context)
        {
            SyntaxToken keywordToken = context.Current.Peek();

            if (keywordToken.Kind == TokenKind.Name)
            {
                switch (keywordToken.Value)
                {
                    case Keywords.Schema:
                        return ParseSchemaExtension(context);
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
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectSchemaKeyword();

            List<DirectiveNode> directives =
                ParseDirectives(context, true);

            List<OperationTypeDefinitionNode> operationTypeDefinitions =
                ParseOperationTypeDefinitions(context);

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
            ParserContext context)
        {
            if (context.Current.IsLeftBrace())
            {
                return ParseMany(context,
                    TokenKind.LeftBrace,
                    ParseOperationTypeDefinition,
                    TokenKind.RightBrace);
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
