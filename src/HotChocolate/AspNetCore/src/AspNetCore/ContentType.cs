using System;

namespace HotChocolate.AspNetCore
{
    public static class ContentType
    {
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

        public static ReadOnlySpan<char> JsonSpan() => _jsonChars;
    }
}
