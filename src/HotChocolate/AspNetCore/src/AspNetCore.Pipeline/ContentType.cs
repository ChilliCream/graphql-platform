using System.Runtime.CompilerServices;

namespace HotChocolate.AspNetCore;

internal static class ContentType
{
    private const string Utf8 = "charset=utf-8";
    private const string Boundary = "boundary=\"-\"";
    public const string GraphQL = $"{Types.Application}/{SubTypes.GraphQL}; {Utf8}";
    public const string Json = $"{Types.Application}/{SubTypes.Json}; {Utf8}";
    public const string MultiPartMixed = $"{Types.MultiPart}/{SubTypes.Mixed}; {Boundary}";
    public const string GraphQLResponse = $"{Types.Application}/{SubTypes.GraphQLResponse}; {Utf8}";
    public const string GraphQLResponseStream = $"{Types.Application}/{SubTypes.GraphQLResponseStream}; {Utf8}";
    public const string EventStream = $"{Types.Text}/{SubTypes.EventStream}; {Utf8}";
    public const string JsonLines = $"{Types.Application}/{SubTypes.JsonLines}; {Utf8}";
    public const string Html = $"{Types.Text}/{SubTypes.Html}";

    private static readonly char[] s_jsonArray =
    [
        'a', 'p', 'p', 'l', 'i', 'c', 'a', 't', 'i', 'o', 'n', '/', 'j', 's', 'o', 'n'
    ];

    private static readonly char[] s_multiPartFormArray =
    [
        'm', 'u', 'l', 't', 'i', 'p', 'a', 'r', 't', '/', 'f', 'o', 'r', 'm', '-', 'd', 'a',
        't', 'a'
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> JsonSpan() => s_jsonArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> MultiPartFormSpan() => s_multiPartFormArray;

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
        public const string GraphQLResponseStream = "graphql-response+jsonl";
        public const string Json = "json";
        public const string JsonLines = "jsonl";
        public const string Mixed = "mixed";
        public const string EventStream = "event-stream";
        public const string Html = "html";
    }
}
