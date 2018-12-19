using System.Collections.Generic;

namespace HotChocolate.Language
{
    // Implements the parsing rules in the Type Definition section.
    public partial class Parser
    {
        /// <summary>
        /// Parses a type definition.
        /// <see cref="ITypeSystemDefinitionNode" />:
        /// TypeSystemDefinition:
        /// - SchemaDefinition
        /// - TypeDefinition
        /// - TypeExtension
        /// - DirectiveDefinition
        ///
        /// TypeDefinition:
        /// - ScalarTypeDefinition
        /// - ObjectTypeDefinition
        /// - InterfaceTypeDefinition
        /// - UnionTypeDefinition
        /// - EnumTypeDefinition
        /// - InputObjectTypeDefinition
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static ITypeSystemDefinitionNode ParseTypeSystemDefinition(
            ParserContext context)
        {
            // Many definitions begin with a description and require a lookahead.
            SyntaxToken keywordToken = context.Current;
            if (keywordToken.IsDescription())
            {
                keywordToken = keywordToken.Peek();
            }

            if (keywordToken.IsName())
            {
                switch (keywordToken.Value)
                {
                    case Keywords.Schema:
                        return ParseSchemaDefinition(context);
                    case Keywords.Scalar:
                        return ParseScalarTypeDefinition(context);
                    case Keywords.Type:
                        return ParseObjectTypeDefinition(context);
                    case Keywords.Interface:
                        return ParseInterfaceTypeDefinition(context);
                    case Keywords.Union:
                        return ParseUnionTypeDefinition(context);
                    case Keywords.Enum:
                        return ParseEnumTypeDefinition(context);
                    case Keywords.Input:
                        return ParseInputObjectTypeDefinition(context);
                    case Keywords.Extend:
                        return ParseTypeExtension(context);
                    case Keywords.Directive:
                        return ParseDirectiveDefinition(context);
                }
            }

            throw context.Unexpected(keywordToken);
        }

        /// <summary>
        /// Parses a description.
        /// <see cref="StringValueNode" />:
        /// StringValue
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static StringValueNode ParseDescription(ParserContext context)
        {
            if (context.Current.IsDescription())
            {
                return ParseStringLiteral(context);
            }
            return null;
        }

        /// <summary>
        /// Parses a schema definition.
        /// <see cref="SchemaDefinitionNode" />:
        /// schema Directives[isConstant:true]? { OperationTypeDefinition+ }
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static SchemaDefinitionNode ParseSchemaDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            context.SkipDescription();
            context.ExpectSchemaKeyword();

            List<DirectiveNode> directives =
                ParseDirectives(context, true);

            List<OperationTypeDefinitionNode> operationTypeDefinitions =
                ParseMany(context,
                    TokenKind.LeftBrace,
                    ParseOperationTypeDefinition,
                    TokenKind.RightBrace);

            Location location = context.CreateLocation(start);

            return new SchemaDefinitionNode
            (
                location,
                directives,
                operationTypeDefinitions
            );
        }

        /// <summary>
        /// Parses an operation type definition.
        /// <see cref="OperationTypeDefinitionNode" />:
        /// OperationType : NamedType
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static OperationTypeDefinitionNode ParseOperationTypeDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            OperationType operation = ParseOperationType(context);
            context.ExpectColon();
            NamedTypeNode type = ParseNamedType(context);
            Location location = context.CreateLocation(start);

            return new OperationTypeDefinitionNode
            (
                location,
                operation,
                type
            );
        }

        /// <summary>
        /// Parses a scalar type definition.
        /// <see cref="ScalarTypeDefinitionNode" />:
        /// Description?
        /// scalar Name Directives[isConstant=true]?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static ScalarTypeDefinitionNode ParseScalarTypeDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectScalarKeyword();
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            Location location = context.CreateLocation(start);

            return new ScalarTypeDefinitionNode
            (
                location,
                name,
                description,
                directives
            );
        }

        /// <summary>
        /// Parses an object type definition.
        /// <see cref="ObjectTypeDefinitionNode" />:
        /// Description?
        /// type Name ImplementsInterfaces? Directives[isConstant=true]? FieldsDefinition?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static ObjectTypeDefinitionNode ParseObjectTypeDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectTypeKeyword();
            NameNode name = ParseName(context);
            List<NamedTypeNode> interfaces =
                ParseImplementsInterfaces(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            List<FieldDefinitionNode> fields =
                ParseFieldsDefinition(context);
            Location location = context.CreateLocation(start);

            return new ObjectTypeDefinitionNode
            (
                location,
                name,
                description,
                directives,
                interfaces,
                fields
            );
        }

        /// <summary>
        /// Parses implementing interfaces.
        /// <see cref="List{NamedTypeNode}" />:
        /// implements &amp;? NamedType
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static List<NamedTypeNode> ParseImplementsInterfaces(
            ParserContext context)
        {
            var list = new List<NamedTypeNode>();

            if (context.SkipKeyword(Keywords.Implements))
            {
                // skip optional leading amperdand.
                context.Skip(TokenKind.Ampersand);

                do
                {
                    list.Add(ParseNamedType(context));
                }
                while (context.Skip(TokenKind.Ampersand));
            }

            return list;
        }

        /// <summary>
        /// Parses field definitions of an interface type or object type
        /// <see cref="IReadOnlyCollection{FieldDefinitionNode}" />:
        /// { FieldDefinition+ }
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static List<FieldDefinitionNode> ParseFieldsDefinition(
            ParserContext context)
        {
            if (context.Current.IsLeftBrace())
            {
                return ParseMany(context,
                    TokenKind.LeftBrace,
                    ParseFieldDefinition,
                    TokenKind.RightBrace);
            }
            return new List<FieldDefinitionNode>();
        }

        /// <summary>
        /// Parses a interface type or object type field definition.
        /// <see cref="FieldDefinitionNode" />:
        /// Description?
        /// Name ArgumentsDefinition? : Type Directives[isConstant=true]?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static FieldDefinitionNode ParseFieldDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            NameNode name = ParseName(context);
            List<InputValueDefinitionNode> arguments =
                ParseArgumentDefinitions(context);
            context.ExpectColon();
            ITypeNode type = ParseTypeReference(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            Location location = context.CreateLocation(start);

            return new FieldDefinitionNode
            (
                location,
                name,
                description,
                arguments,
                type,
                directives
            );
        }

        /// <summary>
        /// Parses field arguments.
        /// <see cref="List{InputValueDefinitionNode}" />:
        /// ( InputValueDefinition+ )
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static List<InputValueDefinitionNode> ParseArgumentDefinitions(
            ParserContext context)
        {
            if (context.Current.IsLeftParenthesis())
            {
                return ParseMany(context,
                    TokenKind.LeftParenthesis,
                    ParseInputValueDefinition,
                    TokenKind.RightParenthesis);
            }
            return new List<InputValueDefinitionNode>();
        }

        /// <summary>
        /// Parses input value definitions.
        /// <see cref="InputValueDefinitionNode" />:
        /// Description? Name : Type DefaultValue? Directives[isConstant=true]?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static InputValueDefinitionNode ParseInputValueDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            NameNode name = ParseName(context);
            context.ExpectColon();
            ITypeNode type = ParseTypeReference(context);
            IValueNode defaultValue = null;
            if (context.Skip(TokenKind.Equal))
            {
                defaultValue = ParseConstantValue(context);
            }
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            Location location = context.CreateLocation(start);

            return new InputValueDefinitionNode
            (
                location,
                name,
                description,
                type,
                defaultValue,
                directives
            );
        }

        /// <summary>
        /// Parses an interface type definition.
        /// <see cref="InterfaceTypeDefinition" />:
        /// Description? interface Name Directives[isConstant=true]? 
        /// FieldsDefinition?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static InterfaceTypeDefinitionNode ParseInterfaceTypeDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectInterfaceKeyword();
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            List<FieldDefinitionNode> fields =
                ParseFieldsDefinition(context);
            Location location = context.CreateLocation(start);

            return new InterfaceTypeDefinitionNode
            (
                location,
                name,
                description,
                directives,
                fields
            );
        }

        /// <summary>
        /// Parses an union type definition.
        /// <see cref="UnionTypeDefinitionNode" />:
        /// Description? union Name Directives[isConstant=true]? 
        /// UnionMemberTypes?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static UnionTypeDefinitionNode ParseUnionTypeDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectUnionKeyword();
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            List<NamedTypeNode> types =
                ParseUnionMemberTypes(context);
            Location location = context.CreateLocation(start);

            return new UnionTypeDefinitionNode
            (
                location,
                name,
                description,
                directives,
                types
            );
        }

        /// <summary>
        /// Parses the union member types.
        /// <see cref="List{NamedTypeNode}" />:
        /// = `|`? NamedType
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static List<NamedTypeNode> ParseUnionMemberTypes(
            ParserContext context)
        {
            var list = new List<NamedTypeNode>();

            if (context.Skip(TokenKind.Equal))
            {
                // skip optional leading pipe
                context.Skip(TokenKind.Pipe);

                do
                {
                    list.Add(ParseNamedType(context));
                }
                while (context.Skip(TokenKind.Pipe));
            }

            return list;
        }

        /// <summary>
        /// Parses an enum type definition.
        /// <see cref="EnumTypeDefinitionNode" />:
        /// Description? enum Name Directives[Const]? EnumValuesDefinition?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static EnumTypeDefinitionNode ParseEnumTypeDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectEnumKeyword();
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            List<EnumValueDefinitionNode> values =
                ParseEnumValuesDefinition(context);
            Location location = context.CreateLocation(start);

            return new EnumTypeDefinitionNode
            (
                location,
                name,
                description,
                directives,
                values
            );
        }

        /// <summary>
        /// Parses the value definitions of an enum type definition.
        /// <see cref="List{EnumValueDefinitionNode}" />:
        /// { EnumValueDefinition+ }
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static List<EnumValueDefinitionNode> ParseEnumValuesDefinition(
            ParserContext context)
        {
            if (context.Current.IsLeftBrace())
            {
                return ParseMany(
                    context,
                    TokenKind.LeftBrace,
                    ParseEnumValueDefinition,
                    TokenKind.RightBrace);
            }
            return new List<EnumValueDefinitionNode>();
        }

        /// <summary>
        /// Parses an enum value definitions.
        /// <see cref="EnumValueDefinitionNode" />:
        /// Description? EnumValue Directives[isConstant=true]?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static EnumValueDefinitionNode ParseEnumValueDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            Location location = context.CreateLocation(start);

            return new EnumValueDefinitionNode
            (
                location,
                name,
                description,
                directives
            );
        }

        private static InputObjectTypeDefinitionNode ParseInputObjectTypeDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectInputKeyword();
            NameNode name = ParseName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, true);
            List<InputValueDefinitionNode> fields =
                ParseInputFieldsDefinition(context);
            Location location = context.CreateLocation(start);

            return new InputObjectTypeDefinitionNode
            (
                location,
                name,
                description,
                directives,
                fields
            );
        }

        private static List<InputValueDefinitionNode> ParseInputFieldsDefinition(
            ParserContext context)
        {
            if (context.Current.IsLeftBrace())
            {
                return ParseMany(
                    context,
                    TokenKind.LeftBrace,
                    ParseInputValueDefinition,
                    TokenKind.RightBrace);
            }
            return new List<InputValueDefinitionNode>();
        }
    }
}
