using System;
using HotChocolate.Language.Properties;
using System.Buffers;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLParser
    {
        public static class Syntax
        {
            public static FieldNode ParseField(
                string sourceText) =>
                Parse<FieldNode>(
                    sourceText,
                    parser => parser.ParseField());

            public static SelectionSetNode ParseSelectionSet
            (string sourceText) =>
                Parse<SelectionSetNode>(
                    sourceText,
                    parser => parser.ParseSelectionSet());

            public static IValueNode ParseValueLiteral(
                string sourceText, 
                bool constant = true) =>
                Parse<IValueNode>(
                    sourceText,
                    parser => parser.ParseValueLiteral(constant));

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

                int length = checked(sourceText.Length * 4);
                bool useStackalloc =
                    length <= GraphQLConstants.StackallocThreshold;

                byte[]? source = null;

                Span<byte> sourceSpan = useStackalloc
                    ? stackalloc byte[length]
                    : (source = ArrayPool<byte>.Shared.Rent(length));

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
        }

        private delegate T ParseSyntax<T>(Utf8GraphQLParser parser) where T : ISyntaxNode;
    }
}
