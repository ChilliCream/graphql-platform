using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Language
{
    public sealed partial class Parser
    {
        private readonly Lexer _lexer;

        public Parser()
        {
            _lexer = new Lexer();
        }

        public Parser(Lexer lexer)
        {
            if (lexer == null)
            {
                throw new ArgumentNullException(nameof(lexer));
            }

            _lexer = lexer;
        }

        public DocumentNode Parse(ISource source)
        {
            return Parse(source, ParserOptions.Default);
        }

        public DocumentNode Parse(ISource source, ParserOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            SyntaxToken start = _lexer.Read(source);
            if (start.Kind != TokenKind.StartOfFile)
            {
                throw new InvalidOperationException(
                    "The first token must be a start of file token.");
            }
            return ParseDocument(source, start,
                options ?? ParserOptions.Default);
        }

        private static DocumentNode ParseDocument(
            ISource source,
            SyntaxToken start,
            ParserOptions options)
        {
            List<IDefinitionNode> definitions = new List<IDefinitionNode>();
            ParserContext context = new ParserContext(
                source, start, options, ParseName);

            context.MoveNext();

            while (!context.IsEndOfFile())
            {
                definitions.Add(ParseDefinition(context));
            }

            Location location = context.CreateLocation(start);

            return new DocumentNode(location, definitions.AsReadOnly());
        }

        private static IDefinitionNode ParseDefinition(ParserContext context)
        {
            SyntaxToken token = context.Current;
            if (token.IsDescription())
            {
                token = token.Peek();
            }

            if (token.IsName())
            {
                switch (token.Value)
                {
                    case Keywords.Query:
                    case Keywords.Mutation:
                    case Keywords.Subscription:
                    case Keywords.Fragment:
                        return ParseExecutableDefinition(context);

                    case Keywords.Schema:
                    case Keywords.Scalar:
                    case Keywords.Type:
                    case Keywords.Interface:
                    case Keywords.Union:
                    case Keywords.Enum:
                    case Keywords.Input:
                    case Keywords.Extend:
                    case Keywords.Directive:
                        return ParseTypeSystemDefinition(context);
                }
            }
            else if (token.IsLeftBrace())
            {
                return ParseExecutableDefinition(context);
            }
            else if (token.IsDescription())
            {
                return ParseTypeSystemDefinition(context);
            }

            throw context.Unexpected(token);
        }

        private static IExecutableDefinitionNode ParseExecutableDefinition(
            ParserContext context)
        {
            if (context.Current.IsName())
            {
                switch (context.Current.Value)
                {
                    case Keywords.Query:
                    case Keywords.Mutation:
                    case Keywords.Subscription:
                        return ParseOperationDefinition(context);

                    case Keywords.Fragment:
                        return ParseFragmentDefinition(context);
                }
            }
            else if (context.Current.IsLeftBrace())
            {
                return ParseOperationDefinition(context);
            }

            throw context.Unexpected(context.Current);
        }

        public static Parser Default { get; } = new Parser();
    }
}
