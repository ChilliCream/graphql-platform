using System;

namespace HotChocolate.Server
{
    public static class ContentType
    {
        private static readonly char[] _graphQLChars = new char[]
        {
            'a',
            'p',
            'p',
            'l',
            'i',
            'c',
            'a',
            't',
            'i',
            'o',
            'n',
            '/',
            'g',
            'r',
            'a',
            'p',
            'h',
            'q',
            'l'
        };

        private static readonly char[] _jsonChars = new char[]
        {
            'a',
            'p',
            'p',
            'l',
            'i',
            'c',
            'a',
            't',
            'i',
            'o',
            'n',
            '/',
            'j',
            's',
            'o',
            'n'
        };

        public const string GraphQL = "application/graphql";

        public const string Json = "application/json";

        public static ReadOnlySpan<char> GraphQLSpan() => _graphQLChars;

        public static ReadOnlySpan<char> JsonSpan() => _jsonChars;
    }
}
