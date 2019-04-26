namespace HotChocolate.Language
{
    internal static class GraphQLKeywords
    {
        // type system
        public static readonly byte[] Schema = new byte[]
        {
            (byte)'s',
            (byte)'c',
            (byte)'h',
            (byte)'e',
            (byte)'m',
            (byte)'a'
        };

        public static readonly byte[] Scalar = new byte[]
        {
            (byte)'s',
            (byte)'c',
            (byte)'a',
            (byte)'l',
            (byte)'a',
            (byte)'r'
        };

        public static readonly byte[] Type = new byte[]
        {
            (byte)'t',
            (byte)'y',
            (byte)'p',
            (byte)'e'
        };

        public static readonly byte[] Interface = new byte[]
        {
            (byte)'i',
            (byte)'n',
            (byte)'t',
            (byte)'e',
            (byte)'r',
            (byte)'f',
            (byte)'a',
            (byte)'c',
            (byte)'e'
        };

        public static readonly byte[] Union = new byte[]
        {
            (byte)'u',
            (byte)'n',
            (byte)'i',
            (byte)'o',
            (byte)'n'
        };

        public static readonly byte[] Enum = new byte[]
        {
            (byte)'e',
            (byte)'n',
            (byte)'u',
            (byte)'m'
        };

        public static readonly byte[] Input = new byte[]
        {
            (byte)'i',
            (byte)'n',
            (byte)'p',
            (byte)'u',
            (byte)'t'
        };

        public static readonly byte[] Extend = new byte[]
        {
            (byte)'e',
            (byte)'x',
            (byte)'t',
            (byte)'e',
            (byte)'n',
            (byte)'d'
        };

        public static readonly byte[] Implements = new byte[]
        {
            (byte)'i',
            (byte)'m',
            (byte)'p',
            (byte)'l',
            (byte)'e',
            (byte)'m',
            (byte)'e',
            (byte)'n',
            (byte)'t',
            (byte)'s'
        };

        public static readonly byte[] Repeatable = new byte[]
        {
            (byte)'r',
            (byte)'e',
            (byte)'p',
            (byte)'e',
            (byte)'a',
            (byte)'t',
            (byte)'a',
            (byte)'b',
            (byte)'l',
            (byte)'e'
        };

        public static readonly byte[] Directive = new byte[]
        {
            (byte)'d',
            (byte)'i',
            (byte)'r',
            (byte)'e',
            (byte)'c',
            (byte)'t',
            (byte)'i',
            (byte)'v',
            (byte)'e'
        };

        // query
        public static readonly byte[] Query = new byte[]
        {
            (byte)'q',
            (byte)'u',
            (byte)'e',
            (byte)'r',
            (byte)'y'
        };

        public static readonly byte[] Mutation = new byte[]
        {
            (byte)'m',
            (byte)'u',
            (byte)'t',
            (byte)'a',
            (byte)'t',
            (byte)'i',
            (byte)'o',
            (byte)'n'
        };

        public static readonly byte[] Subscription = new byte[]
        {
            (byte)'s',
            (byte)'u',
            (byte)'b',
            (byte)'s',
            (byte)'c',
            (byte)'r',
            (byte)'i',
            (byte)'p',
            (byte)'t',
            (byte)'i',
            (byte)'o',
            (byte)'n'
        };

        public static readonly byte[] Fragment = new byte[]
        {
            (byte)'f',
            (byte)'r',
            (byte)'a',
            (byte)'g',
            (byte)'m',
            (byte)'e',
            (byte)'n',
            (byte)'t'
        };

        // general
        public static readonly byte[] On = new byte[]
        {
            (byte)'o',
            (byte)'n'
        };

        public static readonly byte[] True = new byte[]
        {
            (byte)'t',
            (byte)'r',
            (byte)'u',
            (byte)'e'
        };

        public static readonly byte[] False = new byte[]
        {
            (byte)'f',
            (byte)'a',
            (byte)'l',
            (byte)'s',
            (byte)'e'
        };

        public static readonly byte[] Null = new byte[]
        {
            (byte)'n',
            (byte)'u',
            (byte)'l',
            (byte)'l'
        };
    }
}
