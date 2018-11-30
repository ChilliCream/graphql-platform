using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching
{
    internal static class SelectionPathParser
    {
        public static SelectionPath Parse(ISource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            SyntaxToken start = Lexer.Default.Read(RemoveDots(source));
            if (start.Kind != TokenKind.StartOfFile)
            {
                throw new InvalidOperationException(
                    "The first token must be a start of file token.");
            }

            return ParseSelectionPath(source, start, ParserOptions.Default);
        }

        // TODO : we have to fix this on another way without having to duplicate to much lexer code.
        private static ISource RemoveDots(ISource source)
        {
            var stringBuilder = new StringBuilder();

            for (int i = 0; i < source.Text.Length; i++)
            {
                char current = source.Text[i];
                if (current.IsDot())
                {
                    stringBuilder.Append(' ');
                }
                else
                {
                    stringBuilder.Append(current);
                }
            }

            return new Source(stringBuilder.ToString());
        }

        private static SelectionPath ParseSelectionPath(
            ISource source,
            SyntaxToken start,
            ParserOptions options)
        {
            var components = new List<SelectionPathComponent>();
            ParserContext context = new ParserContext(
                source, start, options, Parser.ParseName);

            context.MoveNext();

            while (!context.IsEndOfFile())
            {
                context.Skip(TokenKind.Pipe);
                components.Add(ParseSelectionPathComponent(context));
            }

            return new SelectionPath(components);
        }

        private static SelectionPathComponent ParseSelectionPathComponent(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
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

        private static VariableNode ParseVariable(ParserContext context)
        {
            SyntaxToken start = context.ExpectDollar();
            NameNode group = Parser.ParseName(context);
            context.Expect(TokenKind.Colon);
            NameNode name = Parser.ParseName(context);
            Language.Location location = context.CreateLocation(start);

            return new VariableNode
            (
                location,
                new NameNode(group.Value + ":" + name.Value)
            );
        }
    }
}
