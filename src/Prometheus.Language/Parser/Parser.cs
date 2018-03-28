using System;
using System.Collections.Generic;
using System.Linq;

namespace Prometheus.Language
{
    public class Parser
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
                throw new InvalidOperationException("The first token must be a start of file token.");
            }
            return ParseDocument(source, start);
        }

        private DocumentNode ParseDocument(ISource source, Token start)
        {
            List<IDefinitionNode> definitions = new List<IDefinitionNode>();
            Token current = start;

            while (current.Kind != TokenKind.EndOfFile)
            {
                current = current.Next;
                definitions.Add(ParseDefinition(source, current));
            }

            throw new InvalidOperationException();

        }

        private IDefinitionNode ParseDefinition(ISource source, Token token)
        {
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
                        throw new InvalidOperationException();

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

            throw new InvalidOperationException();

            // throw unexpected(lexer);
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
        private ITypeSystemDefinitionNode parseTypeSystemDefinition(IParserContext context)
        {
            // Many definitions begin with a description and require a lookahead.
            Token keywordToken = context.Token.IsDescription()
                ? context.Peek() : context.Token;

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
                        return parseInterfaceTypeDefinition(lexer);
                    case Keywords.Union:
                        return parseUnionTypeDefinition(lexer);
                    case Keywords.Enum:
                        return parseEnumTypeDefinition(lexer);
                    case Keywords.Input:
                        return parseInputObjectTypeDefinition(lexer);
                    case Keywords.Extend:
                        return parseTypeExtension(lexer);
                    case Keywords.Directive:
                        return parseDirectiveDefinition(lexer);
                }
            }

            throw context.Unexpected(keywordToken);
        }


        private SchemaDefinitionNode ParseSchemaDefinition(
            IParserContext context)
        {
            Token start = context.Token;
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

        private ScalarTypeDefinitionNode ParseScalarTypeDefinition(IParserContext context)
        {
            Token start = context.Token;
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

        private ObjectTypeDefinitionNode ParseObjectTypeDefinition(IParserContext context)
        {
            Token start = context.Token;
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

        private IEnumerable<NamedTypeNode> ParseImplementsInterfaces(IParserContext context)
        {
            if (context.Token.Value == Keywords.Implements)
            {
                context.MoveNext();

                while (context.Skip(TokenKind.Ampersand))
                {
                    yield return ParseNamedType(context);
                }
            }
        }

        private IEnumerable<FieldDefinitionNode> ParseFieldsDefinition(
            IParserContext context)
        {
            if (context.Peek(TokenKind.LeftBrace))
            {
                return ParseMany(context,
                    TokenKind.LeftBrace,
                    ParseFieldDefinition,
                    TokenKind.RightBrace);
            }
            return Enumerable.Empty<FieldDefinitionNode>();
        }

        private FieldDefinitionNode ParseFieldDefinition(IParserContext context)
        {
            Token start = context.Token;
            StringValueNode description = ParseDescription(context);
            NameNode name = ParseName(context);
            InputValueDefinitionNode[] arguments = ParseArgumentDefinitions(context).ToArray(); ;
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
            Token start = context.Token;
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
            Token start = context.Token;
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
                context.Unexpected(context.Token.Previous);
            }

            return type;
        }

        private IEnumerable<DirectiveNode> ParseDirectives(IParserContext context, bool isConstant)
        {
            while (context.Peek(TokenKind.At))
            {
                yield return ParseDirective(context, isConstant);
            }
        }

        private DirectiveNode ParseDirective(
            IParserContext context, bool isConstant)
        {
            Token start = context.Token;
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
            if (context.Token.Next.IsLeftParenthesis())
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

        private ArgumentNode ParseArgument(IParserContext context, Func<IParserContext, IValueNode> parseValue)
        {
            Token start = context.Token;
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

        private IValueNode ParseValueLiteral(IParserContext context, bool isConstant)
        {
            throw new NotImplementedException();
        }

        private IValueNode ParseConstantValue(IParserContext context)
        {
            throw new NotImplementedException();
        }

        private OperationTypeDefinitionNode ParseOperationTypeDefinition(IParserContext context)
        {
            Token start = context.Token;
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
            Token start = context.Token;
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
            if (context.Peek(t => t.IsDescription()))
            {
                return ParseStringLiteral(lexer);
            }
            return null;
        }

        private IEnumerable<T> ParseMany<T>(
            IParserContext context,
            TokenKind openKind,
            Func<IParserContext, T> parser,
            TokenKind closeKind)
        {
            if (context.Token.Kind != openKind)
            {
                throw new SyntaxException(context,
                    $"Expected a name token: {context.Token}.");
            }

            while (context.Skip(closeKind)) // todo : fix this
            {
                yield return parser(context);
            }
        }
    }
}