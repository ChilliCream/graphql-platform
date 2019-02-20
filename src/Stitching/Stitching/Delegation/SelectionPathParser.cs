using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Stitching.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Delegation
{
    internal static class SelectionPathParser
    {
        public static IImmutableStack<SelectionPathComponent> Parse(
            string serializedPath)
        {
            return Parse(new Source(serializedPath));
        }

        public static IImmutableStack<SelectionPathComponent> Parse(
            ISource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            SyntaxToken start = Lexer.Default.Read(RemoveDots(source));
            if (start.Kind != TokenKind.StartOfFile)
            {
                throw new InvalidOperationException(StitchingResources
                    .SelectionPathParser_StartOfFileTokenExpected);
            }

            return ParseSelectionPath(source, start, ParserOptions.Default);
        }

        private static ISource RemoveDots(ISource source)
        {
            var stringBuilder = new StringBuilder();

            for (int i = 0; i < source.Text.Length; i++)
            {
                char current = source.Text[i];
                stringBuilder.Append(current.IsDot() ? ' ' : current);
            }

            return new Source(stringBuilder.ToString());
        }

        private static ImmutableStack<SelectionPathComponent>
            ParseSelectionPath(
                ISource source,
                SyntaxToken start,
                ParserOptions options)
        {
            ImmutableStack<SelectionPathComponent> path =
                ImmutableStack<SelectionPathComponent>.Empty;
            var context = new ParserContext(
                source, start, options, Parser.ParseName);

            context.MoveNext();

            while (!context.IsEndOfFile())
            {
                context.Skip(TokenKind.Pipe);
                path = path.Push(ParseSelectionPathComponent(context));
            }

            return path;
        }

        private static SelectionPathComponent ParseSelectionPathComponent(
            ParserContext context)
        {
            NameNode name = Parser.ParseName(context);
            List<ArgumentNode> arguments = ParseArguments(context);
            return new SelectionPathComponent(name, arguments);
        }

        private static List<ArgumentNode> ParseArguments(
            ParserContext context)
        {
            return Parser.ParseArguments(context, ParseArgument);
        }

        private static ArgumentNode ParseArgument(ParserContext context)
        {
            return Parser.ParseArgument(context, ParseValueLiteral);
        }

        private static IValueNode ParseValueLiteral(ParserContext context)
        {
            if (context.Current.IsDollar())
            {
                return ParseVariable(context);
            }
            return Parser.ParseValueLiteral(context, true);
        }

        private static ScopedVariableNode ParseVariable(ParserContext context)
        {
            SyntaxToken start = context.ExpectDollar();
            NameNode scope = Parser.ParseName(context);
            context.Expect(TokenKind.Colon);
            NameNode name = Parser.ParseName(context);
            Language.Location location = context.CreateLocation(start);

            return new ScopedVariableNode
            (
                location,
                scope,
                name
            );
        }
    }
}
