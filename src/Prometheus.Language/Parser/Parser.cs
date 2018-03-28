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
            Token keywordToken = context.SkipDescription();

            if (keywordToken.IsName())
            {
                switch (keywordToken.Value)
                {
                    case Keywords.Schema:
                        return ParseSchemaDefinition(context);
                    case Keywords.Scalar:
                        return parseScalarTypeDefinition(lexer);
                    case Keywords.Type:
                        return parseObjectTypeDefinition(lexer);
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

            while (context.Skip(closeKind))
            {
                yield return parser(context);
            }
        }
    }

    public static class ParserContextExtensions
    {
        public static Token ExpectName(this IParserContext context)
        {
            return Expect(context, t => t.IsName());
        }

        public static Token ExpectColon(this IParserContext context)
        {
            return Expect(context, t => t.IsColon());
        }

        public static Token ExpectAt(this IParserContext context)
        {
            return Expect(context, t => t.IsAt());
        }

        public static Token Expect(this IParserContext context, Func<Token, bool> expectation)
        {
            if (expectation(context.Token))
            {
                return context.MoveNext().Previous;
            }

            throw new SyntaxException(context,
                $"Expected a name token: {context.Token}.");
        }

        public static Token ExpectSchemaKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.Schema);
        }

        public static Token ExpectKeyword(IParserContext context, string keyword)
        {
            Token token = context.Token;
            if (token.IsName() && token.Value == keyword)
            {
                context.MoveNext();
                return token;
            }
            throw new SyntaxException(context,
                $"Expected \"{keyword}\", found {token}");
        }

        public static Location CreateLocation(this IParserContext context, Token start)
        {
            return null;
        }

        public static SyntaxException Unexpected(this IParserContext context, Token token)
        {
            return new SyntaxException(context, token,
                $"Unexpected token: {token}.");
        }

        public static Token SkipDescription(this IParserContext context)
        {
            if (context.Token.IsDescription())
            {
                return context.MoveNext();
            }
            return context.Token;
        }

        public static bool Skip(this IParserContext context, TokenKind kind)
        {
            Token token = context.MoveNext();
            return token.Kind != kind;
        }

        public static bool Peek(this IParserContext context, TokenKind kind)
        {
            return context.Peek().Kind != kind;
        }

        public static bool Peek(this IParserContext context, Func<Token, bool> condition)
        {
            return condition(context.Peek());
        }
    }

    internal static class Keywords
    {
        public const string Schema = "schema";
        public const string Scalar = "scalar";
        public const string Type = "type";
        public const string Interface = "interface";
        public const string Union = "union";
        public const string Enum = "enum";
        public const string Input = "input";
        public const string Extend = "extend";
        public const string Directive = "directive";
    }


    public static class TokenExtensions
    {
        public static bool IsDescription(this Token token)
        {
            return token.Kind == TokenKind.BlockString
                || token.Kind == TokenKind.String;
        }

        public static bool IsSchema(this Token token)
        {
            return token.Kind == TokenKind.SchemaDefinition;
        }

        public static bool IsName(this Token token)
        {
            return token.Kind == TokenKind.Name;
        }

        public static bool IsAt(this Token token)
        {
            return token.Kind == TokenKind.At;
        }

        public static bool IsColon(this Token token)
        {
            return token.Kind == TokenKind.Colon;
        }

        public static bool IsLeftBrace(this Token token)
        {
            return token.Kind == TokenKind.LeftBrace;
        }

        public static bool IsLeftParenthesis(this Token token)
        {
            return token.Kind == TokenKind.LeftParenthesis;
        }
    }



    internal class ParserSession
    {
        public ParserSession(ISource source, Token firstToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (firstToken == null)
            {
                throw new ArgumentNullException(nameof(firstToken));
            }

            if (firstToken.Kind != TokenKind.StartOfFile)
            {
                throw new ArgumentException("The first token must be a start of file token.");
            }

            Source = source;
            Token = firstToken;
        }

        public ISource Source { get; }

        public Token Token { get; private set; }

        public Token MoveNext()
        {
            Token = Token.Next;
            return Token;
        }

        public Token Peek()
        {
            return Token.Next;
        }
    }


    public interface IParserContext
    {
        ISource Source { get; }

        Token Token { get; }

        Token MoveNext();

        Token Peek();
    }


}