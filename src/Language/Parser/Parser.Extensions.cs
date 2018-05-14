using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Language
{
    public partial class Parser
    {
        private ITypeExtensionNode ParseTypeExtension(ParserContext context)
        {
            SyntaxToken keywordToken = context.Current.Peek();

            if (keywordToken.Kind == TokenKind.Name)
            {
                switch (keywordToken.Value)
                {
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

        private ScalarTypeExtensionNode ParseScalarTypeExtension(
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

        private ObjectTypeExtensionNode ParseObjectTypeExtension(ParserContext context)
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

        private InterfaceTypeExtensionNode ParseInterfaceTypeExtension(ParserContext context)
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

        private UnionTypeExtensionNode ParseUnionTypeExtension(ParserContext context)
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

        private EnumTypeExtensionNode ParseEnumTypeExtension(ParserContext context)
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

        private InputObjectTypeExtensionNode ParseInputObjectTypeExtension(ParserContext context)
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