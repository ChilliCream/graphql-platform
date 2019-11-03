using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Delegation
{
    internal static class SelectionPathParser
    {
        public static IImmutableStack<SelectionPathComponent> Parse(
            string serializedPath)
        {
            if (serializedPath is null)
            {
                throw new ArgumentNullException(nameof(serializedPath));
            }

            byte[] buffer = Encoding.UTF8.GetBytes(RemoveDots(serializedPath));
            var reader = new Utf8GraphQLReader(buffer);
            var parser = new Utf8GraphQLParser(reader, ParserOptions.Default);

            return ParseSelectionPath(ref parser);
        }

        private static string RemoveDots(string serializedPath)
        {
            var stringBuilder = new StringBuilder();

            for (int i = 0; i < serializedPath.Length; i++)
            {
                char current = serializedPath[i];
                stringBuilder.Append(current == GraphQLConstants.Dot ? ' ' : current);
            }

            return stringBuilder.ToString();
        }

        private static ImmutableStack<SelectionPathComponent> ParseSelectionPath(
            ref Utf8GraphQLParser parser)
        {
            var path = ImmutableStack<SelectionPathComponent>.Empty;

            parser.MoveNext();

            while (parser.Kind != TokenKind.EndOfFile)
            {
                path = path.Push(ParseSelectionPathComponent(ref parser));
            }

            return path;
        }

        private static SelectionPathComponent ParseSelectionPathComponent(
            ref Utf8GraphQLParser parser)
        {
            NameNode name = parser.ParseName();
            List<ArgumentNode> arguments = ParseArguments(ref parser);
            return new SelectionPathComponent(name, arguments);
        }

        private static List<ArgumentNode> ParseArguments(
            ref Utf8GraphQLParser parser)
        {
            var list = new List<ArgumentNode>();

            if (parser.Kind == TokenKind.LeftParenthesis)
            {
                // skip opening token
                parser.MoveNext();

                while (parser.Kind != TokenKind.RightParenthesis)
                {
                    list.Add(ParseArgument(ref parser));
                }

                // skip closing token
                parser.ExpectRightParenthesis();

            }
            return list;
        }

        private static ArgumentNode ParseArgument(ref Utf8GraphQLParser parser)
        {
            NameNode name = parser.ParseName();

            parser.ExpectColon();

            IValueNode value = ParseValueLiteral(ref parser);

            return new ArgumentNode
            (
                null,
                name,
                value
            );
        }
        private static IValueNode ParseValueLiteral(ref Utf8GraphQLParser parser)
        {
            if (parser.Kind == TokenKind.Dollar)
            {
                return ParseVariable(ref parser);
            }
            return parser.ParseValueLiteral(true);
        }

        private static ScopedVariableNode ParseVariable(ref Utf8GraphQLParser parser)
        {
            parser.ExpectDollar();
            NameNode scope = parser.ParseName();
            parser.ExpectColon();
            NameNode name = parser.ParseName();

            return new ScopedVariableNode
            (
                null,
                scope,
                name
            );
        }
    }
}
