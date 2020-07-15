using System;

namespace HotChocolate.AspNetCore.Utilities
{
    public static class ContentType
    {
        public const string GraphQL = "application/graphql";

        public const string Json = "application/json";

        public static ReadOnlySpan<char> JsonSpan() => new char[]
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
    }
}
