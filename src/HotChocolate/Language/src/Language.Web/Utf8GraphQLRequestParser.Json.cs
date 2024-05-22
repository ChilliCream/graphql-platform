using System.Buffers;
using static HotChocolate.Language.Properties.LangWebResources;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    public static unsafe object? ParseJson(
        string sourceText,
        ParserOptions? options = null)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            throw new ArgumentException(
                SourceText_Empty,
                nameof(sourceText));
        }

        options ??= ParserOptions.Default;

        var length = checked(sourceText.Length * 4);
        byte[]? source = null;

        var sourceSpan = length <= GraphQLConstants.StackallocThreshold
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
            throw new ArgumentException(SourceText_Empty, nameof(sourceText));
        }

        options ??= ParserOptions.Default;

        var length = checked(sourceText.Length * 4);
        byte[]? source = null;

        var sourceSpan = length <= GraphQLConstants.StackallocThreshold
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

    public static unsafe IReadOnlyList<IReadOnlyDictionary<string, object?>>? ParseVariables(
        string sourceText,
        ParserOptions? options = null)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            throw new ArgumentException(SourceText_Empty, nameof(sourceText));
        }

        options ??= ParserOptions.Default;

        var length = checked(sourceText.Length * 4);
        byte[]? source = null;

        var sourceSpan = length <= GraphQLConstants.StackallocThreshold
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

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>>? ParseVariables(
        ReadOnlySpan<byte> sourceText,
        ParserOptions? options = null)
    {
        options ??= ParserOptions.Default;

        var parser = new Utf8GraphQLRequestParser(sourceText, options);
        parser._reader.Expect(TokenKind.StartOfFile);
        return parser.ParseVariables();
    }

    public static unsafe IReadOnlyDictionary<string, object?>? ParseResponse(
        string sourceText,
        ParserOptions? options = null)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            throw new ArgumentException(SourceText_Empty, nameof(sourceText));
        }

        options ??= ParserOptions.Default;

        var length = checked(sourceText.Length * 4);
        byte[]? source = null;

        var sourceSpan = length <= GraphQLConstants.StackallocThreshold
            ? stackalloc byte[length]
            : source = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            Utf8GraphQLParser.ConvertToBytes(sourceText, ref sourceSpan);
            return ParseResponse(sourceSpan, options);
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

    public static IReadOnlyDictionary<string, object?>? ParseResponse(
        ReadOnlySpan<byte> sourceText,
        ParserOptions? options = null)
    {
        options ??= ParserOptions.Default;

        var parser = new Utf8GraphQLRequestParser(sourceText, options);
        parser._reader.Expect(TokenKind.StartOfFile);
        return parser.ParseResponse();
    }

    public static unsafe IReadOnlyList<object?>? ParseBatchResponse(
        string sourceText,
        ParserOptions? options = null)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            throw new ArgumentException(SourceText_Empty, nameof(sourceText));
        }

        options ??= ParserOptions.Default;

        var length = checked(sourceText.Length * 4);
        byte[]? source = null;

        var sourceSpan = length <= GraphQLConstants.StackallocThreshold
            ? stackalloc byte[length]
            : source = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            Utf8GraphQLParser.ConvertToBytes(sourceText, ref sourceSpan);
            return ParseBatchResponse(sourceSpan, options);
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

    public static IReadOnlyList<object?>? ParseBatchResponse(
        ReadOnlySpan<byte> sourceText,
        ParserOptions? options = null)
    {
        options ??= ParserOptions.Default;

        var parser = new Utf8GraphQLRequestParser(sourceText, options);
        parser._reader.Expect(TokenKind.StartOfFile);
        return parser.ParseBatchResponse();
    }
}
