using System;
using HotChocolate.Language.Properties;
using System.Buffers;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLParser
    {
        public static class Syntax
        {
            /// <summary>
            /// Parses a GraphQL field selection string e.g. field(arg: "abc")
            /// </summary>
            public static FieldNode ParseField(
                string sourceText) =>
                Parse(
                    sourceText,
                    parser => parser.ParseField());

            /// <summary>
            /// Parses a GraphQL field selection string e.g. field(arg: "abc")
            /// </summary>
            public static FieldNode ParseField(
                ReadOnlySpan<byte> sourceText) =>
                Parse(
                    sourceText,
                    parser => parser.ParseField());

            /// <summary>
            /// Parses a GraphQL field selection string e.g. field(arg: "abc")
            /// </summary>
            public static FieldNode ParseField(
                Utf8GraphQLReader reader) =>
                new Utf8GraphQLParser(reader).ParseField();

            /// <summary>
            /// Parses a GraphQL selection set string e.g. { field(arg: "abc") }
            /// </summary>
            public static SelectionSetNode ParseSelectionSet(
                string sourceText) =>
                Parse(
                    sourceText,
                    parser => parser.ParseSelectionSet());

            /// <summary>
            /// Parses a GraphQL selection set string e.g. { field(arg: "abc") }
            /// </summary>
            public static SelectionSetNode ParseSelectionSet(
                ReadOnlySpan<byte> sourceText) =>
                Parse(
                    sourceText,
                    parser => parser.ParseSelectionSet());

            /// <summary>
            /// Parses a GraphQL selection set string e.g. { field(arg: "abc") }
            /// </summary>
            public static SelectionSetNode ParseSelectionSet(
                Utf8GraphQLReader reader) =>
                new Utf8GraphQLParser(reader).ParseSelectionSet();

            public static IValueNode ParseValueLiteral(
                string sourceText,
                bool constant = true) =>
                Parse(
                    sourceText,
                    parser => parser.ParseValueLiteral(constant));

            public static IValueNode ParseValueLiteral(
                ReadOnlySpan<byte> sourceText,
                bool constant = true) =>
                Parse(
                    sourceText,
                    parser => parser.ParseValueLiteral(constant));

            public static IValueNode ParseValueLiteral(
                Utf8GraphQLReader reader,
                bool constant = true) =>
                new Utf8GraphQLParser(reader).ParseValueLiteral(constant);

            public static ObjectValueNode ParseObjectLiteral(
                string sourceText,
                bool constant = true) =>
                Parse(
                    sourceText,
                    parser => parser.ParseObject(constant));

            public static ObjectValueNode ParseObjectLiteral(
                ReadOnlySpan<byte> sourceText,
                bool constant = true) =>
                Parse(
                    sourceText,
                    parser => parser.ParseObject(constant));

            public static ObjectValueNode ParseObjectLiteral(
                Utf8GraphQLReader reader,
                bool constant = true) =>
                new Utf8GraphQLParser(reader).ParseObject(constant);

            private static unsafe T Parse<T>(
                string sourceText,
                ParseSyntax<T> parse,
                bool moveNext = true)
                where T : ISyntaxNode
            {
                if (string.IsNullOrEmpty(sourceText))
                {
                    throw new ArgumentException(
                        LangResources.SourceText_Empty,
                        nameof(sourceText));
                }

                var length = checked(sourceText.Length * 4);
                byte[]? source = null;

                Span<byte> sourceSpan = length <= GraphQLConstants.StackallocThreshold
                    ? stackalloc byte[length]
                    : source = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    ConvertToBytes(sourceText, ref sourceSpan);
                    var parser = new Utf8GraphQLParser(sourceSpan);
                    if (moveNext)
                    {
                        parser.MoveNext();
                    }
                    return parse(parser);
                }
                finally
                {
                    if (source != null)
                    {
                        sourceSpan.Clear();
                        ArrayPool<byte>.Shared.Return(source);
                    }
                }
            }

            private static T Parse<T>(
                ReadOnlySpan<byte> sourceText,
                ParseSyntax<T> parse,
                bool moveNext = true)
                where T : ISyntaxNode
            {
                var parser = new Utf8GraphQLParser(sourceText);
                if (moveNext)
                {
                    parser.MoveNext();
                }
                return parse(parser);
            }
        }

        private delegate T ParseSyntax<T>(Utf8GraphQLParser parser) where T : ISyntaxNode;
    }
}
