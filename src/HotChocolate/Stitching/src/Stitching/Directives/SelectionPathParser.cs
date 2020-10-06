using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    internal static class SelectionPathParser
    {
        private const int _maxStackSize = 256;

        public static IImmutableStack<SelectionPathComponent> Parse(string path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            byte[]? rented = null;
            Span<byte> buffer = path.Length < _maxStackSize
                ? stackalloc byte[path.Length]
                : rented = ArrayPool<byte>.Shared.Rent(path.Length);

            try
            {
                buffer = buffer.Slice(0, path.Length);
                Prepare(path, buffer);
                var reader = new Utf8GraphQLReader(buffer);
                var parser = new Utf8GraphQLParser(reader, ParserOptions.Default);
                return ParseSelectionPath(ref parser);
            }
            finally
            {
                if (rented is not null)
                {
                    buffer.Clear();
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        private static void Prepare(string path, Span<byte> sourceText)
        {
            for (var i = 0; i < path.Length; i++)
            {
                var current = path[i];
                sourceText[i] = current == GraphQLConstants.Dot ? (byte)' ' : (byte)current;
            }
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
        private static IValueNode ParseValueLiteral(
            ref Utf8GraphQLParser parser)
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
