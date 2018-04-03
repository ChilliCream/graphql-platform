using System;
using System.Collections.Generic;
using System.Linq;

namespace Prometheus.Language
{
    public partial class Parser
        : IParser
    {
        public DocumentNode Parse(ILexer lexer, ISource source)
        {
            if (lexer == null)
            {
                throw new ArgumentNullException(nameof(lexer));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Token start = lexer.Read(source);
            if (start.Kind != TokenKind.StartOfFile)
            {
                throw new InvalidOperationException(
                    "The first token must be a start of file token.");
            }
            return ParseDocument(source, start);
        }

        private DocumentNode ParseDocument(ISource source, Token start)
        {
            List<IDefinitionNode> definitions = new List<IDefinitionNode>();
            ParserContext context = new ParserContext(source, start);

            context.MoveNext();

            while (!context.IsEndOfFile())
            {
                definitions.Add(ParseDefinition(context));
            }

            Location location = context.CreateLocation(start);

            return new DocumentNode(location, definitions.AsReadOnly());
        }

        private IDefinitionNode ParseDefinition(IParserContext context)
        {
            Token token = context.Current.IsDescription()
                ? context.Peek() : context.Current;

            if (token.IsName())
            {
                switch (token.Value)
                {
                    case "query":
                    case "mutation":
                    case "subscription":
                    case "fragment":
                        throw new InvalidOperationException();
                    // return parseExecutableDefinition(lexer);

                    case "schema":
                    case "scalar":
                    case "type":
                    case "interface":
                    case "union":
                    case "enum":
                    case "input":
                    case "extend":
                    case "directive":
                        return ParseTypeSystemDefinition(context);

                        // Note: The schema definition language is an experimental addition.
                        // return parseTypeSystemDefinition(lexer);
                }
            }
            else if (token.IsLeftBrace())
            {
                throw new InvalidOperationException();
                //return parseExecutableDefinition(lexer);
            }
            else if (token.IsDescription())
            {
                throw new InvalidOperationException();

                // Note: The schema definition language is an experimental addition.
                // return parseTypeSystemDefinition(lexer);
            }

            throw context.Unexpected(token);
        }

        // Implements the parsing rules in the Type Definition section.

        /**
         * TypeSystemDefinition :
         *   - SchemaDefinition
         *   - TypeDefinition
         *   - TypeExtension
         *   - DirectiveDefinition
         *
         * TypeDefinition :
         *   - ScalarTypeDefinition
         *   - ObjectTypeDefinition
         *   - InterfaceTypeDefinition
         *   - UnionTypeDefinition
         *   - EnumTypeDefinition
         *   - InputObjectTypeDefinition
         */
        private ITypeSystemDefinitionNode ParseTypeSystemDefinition(IParserContext context)
        {
            // Many definitions begin with a description and require a lookahead.
            Token keywordToken = context.Current.IsDescription()
                ? context.Peek() : context.Current;

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

        private SchemaDefinitionNode ParseSchemaDefinition(
            IParserContext context)
        {
            Token start = context.Current;
            context.SkipDescription();
            context.ExpectSchemaKeyword();

            var directives = ParseDirectives(context, true).ToArray();

            var operationTypeDefinitions = ParseMany(context,
                TokenKind.LeftBrace,
                ParseOperationTypeDefinition,
                TokenKind.RightBrace).ToArray();

            Location location = context.CreateLocation(start);

            return new SchemaDefinitionNode
            (
                location,
                directives,
                operationTypeDefinitions
            );
        }

        private ScalarTypeDefinitionNode ParseScalarTypeDefinition(
            IParserContext context)
        {
            Token start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectScalarKeyword();
            NameNode name = ParseName(context);
            var directives = ParseDirectives(context, true).ToArray();
            Location location = context.CreateLocation(start);

            return new ScalarTypeDefinitionNode
            (
                location,
                name,
                description,
                directives
            );
        }

        private ObjectTypeDefinitionNode ParseObjectTypeDefinition(
            IParserContext context)
        {
            Token start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectTypeKeyword();
            NameNode name = ParseName(context);
            NamedTypeNode[] interfaces = ParseImplementsInterfaces(context).ToArray();
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            FieldDefinitionNode[] fields = ParseFieldsDefinition(context).ToArray();
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

        private InterfaceTypeDefinitionNode ParseInterfaceTypeDefinition(
            IParserContext context)
        {
            Token start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectInterfaceKeyword();
            NameNode name = ParseName(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            FieldDefinitionNode[] fields = ParseFieldsDefinition(context).ToArray();
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

        private UnionTypeDefinitionNode ParseUnionTypeDefinition(
            IParserContext context)
        {
            Token start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectUnionKeyword();
            NameNode name = ParseName(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            NamedTypeNode[] types = ParseUnionMemberTypes(context).ToArray();
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

        private IEnumerable<NamedTypeNode> ParseUnionMemberTypes(
            IParserContext context)
        {
            if (context.Skip(TokenKind.Equal))
            {
                // skip optional leading pipe
                context.Skip(TokenKind.Pipe);

                do
                {
                    yield return ParseNamedType(context);
                }
                while (context.Skip(TokenKind.Pipe));
            }
        }


        private EnumTypeDefinitionNode ParseEnumTypeDefinition(
            IParserContext context)
        {
            Token start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectEnumKeyword();
            NameNode name = ParseName(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            EnumValueDefinitionNode[] values = ParseEnumValuesDefinition(context).ToArray();
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

        private IEnumerable<EnumValueDefinitionNode> ParseEnumValuesDefinition(
            IParserContext context)
        {
            if (context.Peek(TokenKind.LeftBrace))
            {
                return ParseMany(
                    context,
                    TokenKind.LeftBrace,
                    ParseEnumValueDefinition,
                    TokenKind.RightBrace);
            }
            return Enumerable.Empty<EnumValueDefinitionNode>();
        }

        private EnumValueDefinitionNode ParseEnumValueDefinition(
            IParserContext context)
        {
            Token start = context.Current;
            StringValueNode description = ParseDescription(context);
            NameNode name = ParseName(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            Location location = context.CreateLocation(start);

            return new EnumValueDefinitionNode
            (
                location,
                name,
                description,
                directives
            );
        }

        private InputObjectTypeDefinitionNode ParseInputObjectTypeDefinition(
            IParserContext context)
        {
            Token start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectInputKeyword();
            NameNode name = ParseName(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            InputValueDefinitionNode[] fields = ParseInputFieldsDefinition(context).ToArray();
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

        private IEnumerable<InputValueDefinitionNode> ParseInputFieldsDefinition(
            IParserContext context)
        {
            if (context.Peek(TokenKind.LeftBrace))
            {
                return ParseMany(
                    context,
                    TokenKind.LeftBrace,
                    ParseInputValueDefinition,
                    TokenKind.RightBrace);
            }
            return Enumerable.Empty<InputValueDefinitionNode>();
        }

        private ITypeExtensionNode ParseTypeExtension(IParserContext context)
        {
            Token keywordToken = context.Peek().Next;

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


        private ScalarTypeExtensionNode ParseScalarTypeExtension(IParserContext context)
        {
            Token start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectScalarKeyword();
            NameNode name = ParseName(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            if (directives.Length == 0)
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

        private ObjectTypeExtensionNode ParseObjectTypeExtension(IParserContext context)
        {
            Token start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectTypeKeyword();
            NameNode name = ParseName(context);
            NamedTypeNode[] interfaces = ParseImplementsInterfaces(context).ToArray();
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            FieldDefinitionNode[] fields = ParseFieldsDefinition(context).ToArray();
            Location location = context.CreateLocation(start);

            if (interfaces.Length == 0
                && directives.Length == 0
                && fields.Length == 0)
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

        private InterfaceTypeExtensionNode ParseInterfaceTypeExtension(IParserContext context)
        {
            Token start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectInterfaceKeyword();
            NameNode name = ParseName(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            FieldDefinitionNode[] fields = ParseFieldsDefinition(context).ToArray();
            Location location = context.CreateLocation(start);

            if (directives.Length == 0
                && fields.Length == 0)
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

        private UnionTypeExtensionNode ParseUnionTypeExtension(IParserContext context)
        {
            Token start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectUnionKeyword();
            NameNode name = ParseName(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            NamedTypeNode[] types = ParseUnionMemberTypes(context).ToArray();
            Location location = context.CreateLocation(start);

            if (directives.Length == 0 && types.Length == 0)
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

        private EnumTypeExtensionNode ParseEnumTypeExtension(IParserContext context)
        {
            Token start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectEnumKeyword();
            NameNode name = ParseName(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            EnumValueDefinitionNode[] values = ParseEnumValuesDefinition(context).ToArray();
            Location location = context.CreateLocation(start);

            if (directives.Length == 0 && values.Length == 0)
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

        private InputObjectTypeExtensionNode ParseInputObjectTypeExtension(IParserContext context)
        {
            Token start = context.Current;
            context.ExpectExtendKeyword();
            context.ExpectInputKeyword();
            NameNode name = ParseName(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
            InputValueDefinitionNode[] fields = ParseInputFieldsDefinition(context).ToArray();
            Location location = context.CreateLocation(start);

            if (directives.Length == 0 && fields.Length == 0)
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

        private DirectiveDefinitionNode ParseDirectiveDefinition(IParserContext context)
        {
            Token start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectDirectiveKeyword();
            context.ExpectAt();
            NameNode name = ParseName(context);
            InputValueDefinitionNode[] arguments = ParseArgumentDefinitions(context).ToArray();
            context.ExpectOnKeyword();
            NameNode[] locations = ParseDirectiveLocations(context).ToArray();
            Location location = context.CreateLocation(start);

            return new DirectiveDefinitionNode
            (
                location,
                name,
                description,
                arguments,
                locations
            );
        }

        /**
         * DirectiveLocations :
         *   - `|`? DirectiveLocation
         *   - DirectiveLocations | DirectiveLocation
         */
        private IEnumerable<NameNode> ParseDirectiveLocations(IParserContext context)
        {
            // skip optional leading pipe.
            context.Skip(TokenKind.Pipe);

            do
            {
                yield return ParseDirectiveLocation(context);
            }
            while (context.Skip(TokenKind.Pipe));
        }

        /*
         * DirectiveLocation :
         *   - ExecutableDirectiveLocation
         *   - TypeSystemDirectiveLocation
         *
         * ExecutableDirectiveLocation : one of
         *   `QUERY`
         *   `MUTATION`
         *   `SUBSCRIPTION`
         *   `FIELD`
         *   `FRAGMENT_DEFINITION`
         *   `FRAGMENT_SPREAD`
         *   `INLINE_FRAGMENT`
         *
         * TypeSystemDirectiveLocation : one of
         *   `SCHEMA`
         *   `SCALAR`
         *   `OBJECT`
         *   `FIELD_DEFINITION`
         *   `ARGUMENT_DEFINITION`
         *   `INTERFACE`
         *   `UNION`
         *   `ENUM`
         *   `ENUM_VALUE`
         *   `INPUT_OBJECT`
         *   `INPUT_FIELD_DEFINITION`
         */
        private NameNode ParseDirectiveLocation(IParserContext context)
        {
            Token start = context.Current;
            NameNode name = ParseName(context);
            if (DirectiveLocation.IsValidName(name.Value))
            {
                return name;
            }
            throw context.Unexpected(start);
        }

        private IEnumerable<NamedTypeNode> ParseImplementsInterfaces(
            IParserContext context)
        {
            if (context.SkipKeyword(Keywords.Implements))
            {
                // skip optional leading amperdand.
                context.Skip(TokenKind.Ampersand);

                do
                {
                    yield return ParseNamedType(context);
                }
                while (context.Skip(TokenKind.Ampersand));
            }
        }

        private IEnumerable<FieldDefinitionNode> ParseFieldsDefinition(
            IParserContext context)
        {
            if (context.Current.IsLeftBrace())
            {
                return ParseMany(context,
                    TokenKind.LeftBrace,
                    ParseFieldDefinition,
                    TokenKind.RightBrace);
            }
            return Enumerable.Empty<FieldDefinitionNode>();
        }

        private FieldDefinitionNode ParseFieldDefinition(
            IParserContext context)
        {
            Token start = context.Current;
            StringValueNode description = ParseDescription(context);
            NameNode name = ParseName(context);
            InputValueDefinitionNode[] arguments = ParseArgumentDefinitions(context).ToArray();
            context.ExpectColon();
            ITypeNode type = ParseTypeReference(context);
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
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

        private IEnumerable<InputValueDefinitionNode> ParseArgumentDefinitions(
            IParserContext context)
        {
            if (context.Peek(TokenKind.LeftParenthesis))
            {
                return ParseMany(context,
                    TokenKind.LeftParenthesis,
                    ParseInputValueDefinition,
                    TokenKind.RightParenthesis);
            }
            return Enumerable.Empty<InputValueDefinitionNode>();
        }

        private InputValueDefinitionNode ParseInputValueDefinition(
            IParserContext context)
        {
            Token start = context.Current;
            StringValueNode description = ParseDescription(context);
            NameNode name = ParseName(context);
            context.ExpectColon();
            ITypeNode type = ParseTypeReference(context);
            IValueNode defaultValue = null;
            if (context.Skip(TokenKind.Equal))
            {
                defaultValue = ParseConstantValue(context);
            }
            DirectiveNode[] directives = ParseDirectives(context, true).ToArray();
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

        private ITypeNode ParseTypeReference(IParserContext context)
        {
            Token start = context.Current;
            ITypeNode type;
            Location location;

            if (context.Skip(TokenKind.LeftBracket))
            {
                type = ParseTypeReference(context);
                context.ExpectRightBracket();
                location = context.CreateLocation(start);

                type = new ListTypeNode(location, type);
            }
            else
            {
                type = ParseNamedType(context);
            }

            if (context.Skip(TokenKind.Bang))
            {
                if (type is INullableType nt)
                {
                    return new NonNullTypeNode
                    (
                        context.CreateLocation(start),
                        nt
                    );
                }
                context.Unexpected(context.Current.Previous);
            }

            return type;
        }

        private IEnumerable<DirectiveNode> ParseDirectives(
            IParserContext context, bool isConstant)
        {
            while (context.Peek(TokenKind.At))
            {
                yield return ParseDirective(context, isConstant);
            }
        }

        private DirectiveNode ParseDirective(
            IParserContext context, bool isConstant)
        {
            Token start = context.Current;
            context.ExpectAt();
            NameNode name = ParseName(context);
            var arguments = ParseArguments(context, isConstant).ToArray();
            Location location = context.CreateLocation(start);

            return new DirectiveNode
            (
                location,
                name,
                arguments
            );
        }

        private IEnumerable<ArgumentNode> ParseArguments(
            IParserContext context, bool isConstant)
        {
            if (isConstant)
            {
                return ParseArguments(context, ParseConstantArgument);
            }
            return ParseArguments(context, ParseArgument);
        }

        private IEnumerable<ArgumentNode> ParseArguments(
            IParserContext context,
            Func<IParserContext, ArgumentNode> parseArgument)
        {
            if (context.Current.Next.IsLeftParenthesis())
            {
                return ParseMany(
                    context,
                    TokenKind.LeftParenthesis,
                    parseArgument,
                    TokenKind.RightParenthesis);
            }
            return Array.Empty<ArgumentNode>();
        }

        private ArgumentNode ParseConstantArgument(IParserContext context)
        {
            return ParseArgument(context, ParseConstantValue);
        }

        private ArgumentNode ParseArgument(IParserContext context)
        {
            return ParseArgument(context, c => ParseValueLiteral(c, false));
        }

        private ArgumentNode ParseArgument(IParserContext context,
            Func<IParserContext, IValueNode> parseValue)
        {
            Token start = context.Current;
            NameNode name = ParseName(context);
            context.ExpectColon();
            IValueNode value = parseValue(context);
            Location location = context.CreateLocation(start);

            return new ArgumentNode
            (
                location,
                name,
                value
            );
        }

        

        private OperationTypeDefinitionNode ParseOperationTypeDefinition(
            IParserContext context)
        {
            Token start = context.Current;
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

        private OperationType ParseOperationType(IParserContext context)
        {
            Token token = context.ExpectName();

            if (Enum.TryParse(token.Value, true, out OperationType type))
            {
                return type;
            }

            throw context.Unexpected(token);
        }

        private NamedTypeNode ParseNamedType(IParserContext context)
        {
            Token start = context.Current;
            NameNode name = ParseName(context);
            Location location = context.CreateLocation(start);

            return new NamedTypeNode
            (
                location,
                name
            );
        }

        private NameNode ParseName(IParserContext context)
        {
            Token token = context.ExpectName();
            Location location = context.CreateLocation(token);

            return new NameNode
            (
                location,
                token.Value
            );
        }

        private StringValueNode ParseDescription(IParserContext context)
        {
            if (context.Current.IsDescription())
            {
                return ParseStringLiteral(context);
            }
            return null;
        }

        private IEnumerable<T> ParseMany<T>(
            IParserContext context,
            TokenKind openKind,
            Func<IParserContext, T> parser,
            TokenKind closeKind)
        {
            if (context.Current.Kind != openKind)
            {
                throw new SyntaxException(context,
                    $"Expected a name token: {context.Current}.");
            }

            // skip opening token
            context.MoveNext();

            while (context.Current.Kind != closeKind) // todo : fix this
            {
                yield return parser(context);
            }

            // skip closing token
            context.MoveNext();
        }
    }
}