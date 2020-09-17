using System;
using System.Buffers;
using System.Collections.Generic;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        public static unsafe object? ParseJson(
            string sourceText,
            ParserOptions? options = null)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentException(
                    LangResources.SourceText_Empty,
                    nameof(sourceText));
            }

            options ??= ParserOptions.Default;

            var length = checked(sourceText.Length * 4);
            byte[]? source = null;

            Span<byte> sourceSpan = length <= GraphQLConstants.StackallocThreshold
                ? stackalloc byte[length]
                : source = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                Utf8GraphQLParser.ConvertToBytes(sourceText, ref sourceSpan);
                return ParseJson(sourceSpan, options);
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

        public static object? ParseJson(
            ReadOnlySpan<byte> sourceText,
            ParserOptions? options = null)
        {
            options ??= ParserOptions.Default;
            return new Utf8GraphQLRequestParser(sourceText, options).ParseJson();
        }

        public static unsafe IReadOnlyDictionary<string, object?>? ParseJsonObject(
            string sourceText,
            ParserOptions? options = null)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentException(
                    LangResources.SourceText_Empty,
                    nameof(sourceText));
            }

            options ??= ParserOptions.Default;

            var length = checked(sourceText.Length * 4);
            byte[]? source = null;

            Span<byte> sourceSpan = length <= GraphQLConstants.StackallocThreshold
                ? stackalloc byte[length]
                : source = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                Utf8GraphQLParser.ConvertToBytes(sourceText, ref sourceSpan);
                return ParseJsonObject(sourceSpan, options);
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

        public static IReadOnlyDictionary<string, object?>? ParseJsonObject(
            ReadOnlySpan<byte> sourceText,
            ParserOptions? options = null)
        {
            options ??= ParserOptions.Default;
            
            var parser = new Utf8GraphQLRequestParser(sourceText, options);
            parser._reader.Expect(TokenKind.StartOfFile);
            return parser.ParseObjectOrNull();
        }

        public static unsafe IReadOnlyDictionary<string, object?>? ParseVariables(
            string sourceText,
            ParserOptions? options = null)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentException(
                    LangResources.SourceText_Empty,
                    nameof(sourceText));
            }

            options ??= ParserOptions.Default;

            var length = checked(sourceText.Length * 4);
            byte[]? source = null;

            Span<byte> sourceSpan = length <= GraphQLConstants.StackallocThreshold
                ? stackalloc byte[length]
                : source = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                Utf8GraphQLParser.ConvertToBytes(sourceText, ref sourceSpan);
                return ParseVariables(sourceSpan, options);
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

        public static IReadOnlyDictionary<string, object?>? ParseVariables(
            ReadOnlySpan<byte> sourceText,
            ParserOptions? options = null)
        {
            options ??= ParserOptions.Default;

            var parser = new Utf8GraphQLRequestParser(sourceText, options);
            parser._reader.Expect(TokenKind.StartOfFile);
            return parser.ParseVariables();
        }
    }
}
