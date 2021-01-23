using System;

namespace HotChocolate.AspNetCore
{
    internal static class ContentType
    {
        public const string GraphQL = "application/graphql; charset=utf-8";
        public const string Json = "application/json; charset=utf-8";
        public const string MultiPart = "multipart/mixed; boundary=\"-\"";

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

        public static ReadOnlySpan<char> MultiPartSpan() => new char[]
        {
            'm',
            'u',
            'l',
            't',
            'i',
            'p',
            'a',
            'r',
            't',
            '/',
            'f',
            'o',
            'r',
            'm',
            '-',
            'd',
            'a',
            't',
            'a'
        };
    }
}
