using System.Runtime.CompilerServices;

namespace HotChocolate.AspNetCore;

internal static class ContentType
{
    public const string GraphQL = "application/graphql; charset=utf-8";
    public const string Json = "application/json; charset=utf-8";
    public const string MultiPartMixed = "multipart/mixed; boundary=\"-\"";
    public const string GraphQLResponse = "application/graphql-response+json; charset=utf-8";
    public const string EventStream = "text/event-stream; charset=utf-8";

    private static readonly char[] _jsonArray =
    {
        'a', 'p', 'p', 'l', 'i', 'c', 'a', 't', 'i', 'o', 'n', '/', 'j', 's', 'o', 'n'
    };

    private static readonly char[] _multiPartFormArray =
    {
        'm', 'u', 'l', 't', 'i', 'p', 'a', 'r', 't', '/', 'f', 'o', 'r', 'm', '-', 'd', 'a',
        't', 'a'
    };

    private static readonly char[] _multiPartMixedArray =
    {
        'm', 'u', 'l', 't', 'i', 'p', 'a', 'r', 't', '/', 'm', 'i', 'x', 'e', 'd'
    };

    private static readonly char[] _graphqlResponseArray =
    {
        'a', 'p', 'p', 'l', 'i', 'c', 'a', 't', 'i', 'o', 'n', '/', 'g', 'r', 'a', 'p', 'h',
        'q', 'l', '-', 'r', 'e', 's', 'p', 'o', 'n', 's', 'e', '+', 'j', 's', 'o', 'n',
    };

    private static readonly char[] _eventStream =
    {
        't', 'e', 'x', 't', '/', 'e', 'v', 'e', 'n', 't', '-', 's', 't', 'r', 'e', 'a', 'm'
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> JsonSpan() => _jsonArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> MultiPartFormSpan() => _multiPartFormArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> MultiPartMixedSpan() => _multiPartMixedArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GraphQLResponseSpan() => _graphqlResponseArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> EventStreamSpan() => _eventStream;
}
