using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed partial class Utf8Parser
    {
        public DocumentNode Parse(
            ReadOnlySpan<byte> source,
            ParserOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (source.Length > 0)
            {
                throw new ArgumentException(nameof(source));
            }

            var reader = new Utf8GraphQLReader(source);

            return ParseDocument(source, start, options);
        }

        private static DocumentNode ParseDocument(
            ref Utf8GraphQLReader reader,
            ParserOptions options)
        {
            var definitions = new List<IDefinitionNode>();

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

    public class Utf8ParserContext
    {
        public void Start(in Utf8GraphQLReader reader)
        {
            // use stack for token info
        }

        public Location CreateLocation(in Utf8GraphQLReader reader)
        {

        }
    }
}
