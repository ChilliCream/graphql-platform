using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    // Implements the parsing rules in the Type Definition section.
    public partial class Utf8Parser
    {
        /// <summary>
        /// Parses a description.
        /// <see cref="StringValueNode" />:
        /// StringValue
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static StringValueNode ParseDescription(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsDescription(ref reader))
            {
                return ParseStringLiteral(context, ref reader);
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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            ParserHelper.SkipDescription(ref reader);
            ParserHelper.ExpectSchemaKeyword(ref reader);

            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);

            if (reader.Kind != TokenKind.LeftBrace)
            {
                throw new SyntaxException(reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBrace,
                        TokenVisualizer.Visualize(in reader)));
            }

            var operationTypeDefinitions = new List<OperationTypeDefinitionNode>();

            // skip opening token
            ParserHelper.MoveNext(ref reader);

            while (reader.Kind != TokenKind.RightBrace)
            {
                operationTypeDefinitions.Add(
                    ParseOperationTypeDefinition(context, ref reader));
            }

            // skip closing token
            ParserHelper.Expect(ref reader, TokenKind.RightBrace);

            Location location = context.CreateLocation(ref reader);

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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            OperationType operation = ParseOperationType(context, ref reader);
            ParserHelper.ExpectColon(ref reader);
            NamedTypeNode type = ParseNamedType(context, ref reader);

            Location location = context.CreateLocation(ref reader);

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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            // skip scalar keyword
            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            StringValueNode description = context.PopDescription();
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);

            Location location = context.CreateLocation(ref reader);

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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            // skip type keyword
            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            StringValueNode description = context.PopDescription();
            List<NamedTypeNode> interfaces =
                ParseImplementsInterfaces(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            List<FieldDefinitionNode> fields =
                ParseFieldsDefinition(context, ref reader);

            Location location = context.CreateLocation(ref reader);

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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            var list = new List<NamedTypeNode>();

            if (ParserHelper.SkipKeyword(ref reader, Utf8Keywords.Implements))
            {
                // skip optional leading amperdand.
                ParserHelper.Skip(ref reader, TokenKind.Ampersand);

                do
                {
                    list.Add(ParseNamedType(context, ref reader));
                }
                while (ParserHelper.Skip(ref reader, TokenKind.Ampersand));
            }

            return list;
        }

        /// <summary>
        /// Parses field definitions of an interface type or object type
        /// <see cref="IReadOnlyList{FieldDefinitionNode}" />:
        /// { FieldDefinition+ }
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static List<FieldDefinitionNode> ParseFieldsDefinition(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.LeftBrace)
            {
                var list = new List<FieldDefinitionNode>();

                // skip opening token
                ParserHelper.MoveNext(ref reader);

                while (reader.Kind != TokenKind.LeftBrace)
                {
                    list.Add(ParseFieldDefinition(context, ref reader));
                }

                // skip closing token
                ParserHelper.Expect(ref reader, TokenKind.RightBrace);

                return list;
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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            StringValueNode description = ParseDescription(context, ref reader);
            NameNode name = ParseName(context, ref reader);
            List<InputValueDefinitionNode> arguments =
                ParseArgumentDefinitions(context, ref reader);
            ParserHelper.ExpectColon(ref reader);
            ITypeNode type = ParseTypeReference(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);

            Location location = context.CreateLocation(ref reader);

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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.LeftParenthesis)
            {
                var list = new List<InputValueDefinitionNode>();

                // skip opening token
                ParserHelper.MoveNext(ref reader);

                while (reader.Kind != TokenKind.LeftParenthesis)
                {
                    list.Add(ParseInputValueDefinition(context, ref reader));
                }

                // skip closing token
                ParserHelper.Expect(ref reader, TokenKind.RightParenthesis);

                return list;
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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            StringValueNode description = ParseDescription(context, ref reader);
            NameNode name = ParseName(context, ref reader);
            ParserHelper.ExpectColon(ref reader);
            ITypeNode type = ParseTypeReference(context, ref reader);
            IValueNode defaultValue = null;
            if (ParserHelper.Skip(ref reader, TokenKind.Equal))
            {
                defaultValue = ParseValueLiteral(context, ref reader, true);
            }
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);

            Location location = context.CreateLocation(ref reader);

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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            StringValueNode description = context.PopDescription();
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            List<FieldDefinitionNode> fields =
                ParseFieldsDefinition(context, ref reader);

            Location location = context.CreateLocation(ref reader);

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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            StringValueNode description = context.PopDescription();
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            List<NamedTypeNode> types =
                ParseUnionMemberTypes(context, ref reader);

            Location location = context.CreateLocation(ref reader);

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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            var list = new List<NamedTypeNode>();

            if (ParserHelper.Skip(ref reader, TokenKind.Equal))
            {
                // skip optional leading pipe (might not exist!)
                ParserHelper.Skip(ref reader, TokenKind.Pipe);

                do
                {
                    list.Add(ParseNamedType(context, ref reader));
                }
                while (ParserHelper.Skip(ref reader, TokenKind.Pipe));
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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            StringValueNode description = context.PopDescription();
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            List<EnumValueDefinitionNode> values =
                ParseEnumValuesDefinition(context, ref reader);

            Location location = context.CreateLocation(ref reader);

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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.LeftBrace)
            {
                var list = new List<EnumValueDefinitionNode>();

                // skip opening token
                ParserHelper.MoveNext(ref reader);

                while (reader.Kind != TokenKind.LeftBrace)
                {
                    list.Add(ParseEnumValueDefinition(context, ref reader));
                }

                // skip closing token
                ParserHelper.Expect(ref reader, TokenKind.RightBrace);

                return list;
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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            StringValueNode description = ParseDescription(context, ref reader);
            NameNode name = ParseName(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);

            Location location = context.CreateLocation(ref reader);

            return new EnumValueDefinitionNode
            (
                location,
                name,
                description,
                directives
            );
        }

        private static InputObjectTypeDefinitionNode ParseInputObjectTypeDefinition(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            ParserHelper.MoveNext(ref reader);

            NameNode name = ParseName(context, ref reader);
            StringValueNode description = context.PopDescription();
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, true);
            List<InputValueDefinitionNode> fields =
                ParseInputFieldsDefinition(context, ref reader);

            Location location = context.CreateLocation(ref reader);

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
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.LeftBrace)
            {
                var list = new List<InputValueDefinitionNode>();

                // skip opening token
                ParserHelper.MoveNext(ref reader);

                while (reader.Kind != TokenKind.LeftBrace)
                {
                    list.Add(ParseInputValueDefinition(context, ref reader));
                }

                // skip closing token
                ParserHelper.Expect(ref reader, TokenKind.RightBrace);

                return list;
            }
            return new List<InputValueDefinitionNode>();
        }
    }
}
