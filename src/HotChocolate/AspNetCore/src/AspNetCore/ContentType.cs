using System.Runtime.CompilerServices;

namespace HotChocolate.AspNetCore;

internal static class ContentType
{
    private const string _utf8 = "charset=utf-8";
    private const string _boundary = "boundary=\"-\"";
    public const string GraphQL = $"{Types.Application}/{SubTypes.GraphQL}; {_utf8}";
    public const string Json = $"{Types.Application}/{SubTypes.Json}; {_utf8}";
    public const string MultiPartMixed = $"{Types.MultiPart}/{SubTypes.Mixed}; {_boundary}";
    public const string GraphQLResponse = $"{Types.Application}/{SubTypes.GraphQLResponse}; {_utf8}";
    public const string EventStream = $"{Types.Text}/{SubTypes.EventStream}; {_utf8}";
    public const string Html = $"{Types.Text}/{SubTypes.Html}";

    private static readonly char[] _jsonArray =
    [
        'a', 'p', 'p', 'l', 'i', 'c', 'a', 't', 'i', 'o', 'n', '/', 'j', 's', 'o', 'n',
    ];

    private static readonly char[] _multiPartFormArray =
    [
        'm', 'u', 'l', 't', 'i', 'p', 'a', 'r', 't', '/', 'f', 'o', 'r', 'm', '-', 'd', 'a',
        't', 'a',
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> JsonSpan() => _jsonArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> MultiPartFormSpan() => _multiPartFormArray;

    public static class Types
    {
        public const string All = "*";
        public const string Application = "application";
        public const string MultiPart = "multipart";
        public const string Text = "text";
    }

    public static class SubTypes
    {
        public const string GraphQL = "graphql";
        public const string GraphQLResponse = "graphql-response+json";
        public const string Json = "json";
        public const string Mixed = "mixed";
        public const string EventStream = "event-stream";
        public const string Html = "html";
    }
}
