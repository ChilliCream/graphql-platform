using System.Runtime.CompilerServices;

namespace HotChocolate.AspNetCore;

internal static class ContentType
{
    public const string GraphQL = "application/graphql; charset=utf-8";
    public const string Json = "application/json; charset=utf-8";
    public const string MultiPart = "multipart/mixed; boundary=\"-\"";
    public const string GraphQLResponse = "application/graphql-response+json; charset=utf-8";

    private static readonly char[] _jsonArray =
    {
        'a', 'p', 'p', 'l', 'i', 'c', 'a', 't', 'i', 'o', 'n', '/', 'j', 's', 'o', 'n'
    };

    private static readonly char[] _multiPartArray =
    {
        'm', 'u', 'l', 't', 'i', 'p', 'a', 'r', 't', '/', 'f', 'o', 'r', 'm', '-', 'd', 'a',
        't', 'a'
    };

    private static readonly char[] _graphqlResponseArray =
    {
        'a', 'p', 'p', 'l', 'i', 'c', 'a', 't', 'i', 'o', 'n', '/', 'g', 'r', 'a', 'p', 'h',
        'q', 'l', '-', 'r', 'e', 's', 'p', 'o', 'n', 's', 'e', '+', 'j', 's', 'o', 'n',
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> JsonSpan() => _jsonArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> MultiPartSpan() => _multiPartArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GraphQLResponseSpan() => _graphqlResponseArray;
}
