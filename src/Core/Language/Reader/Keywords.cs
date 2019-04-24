namespace HotChocolate.Language
{
    internal static class Utf8Keywords
    {
        // type system
        public const string Schema = "schema";
        public const string Scalar = "scalar";
        public const string Type = "type";
        public const string Interface = "interface";
        public const string Union = "union";
        public const string Enum = "enum";
        public const string Input = "input";
        public const string Extend = "extend";
        public static readonly byte[] Directive = new byte[] { (byte)'d', (byte)'i', (byte)'r', (byte)'e', (byte)'c', (byte)'t', (byte)'i', (byte)'v', (byte)'e' };
        public const string Implements = "implements";
        public const string Repeatable = "repeatable";

        // query
        public const string Query = "query";
        public const string Mutation = "mutation";
        public const string Subscription = "subscription";
        public const string Fragment = "fragment";

        // general
        public static readonly byte[] On = new byte[] { (byte)'o', (byte)'n' };
        public static readonly byte[] True = new byte[] { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };
        public static readonly byte[] False = new byte[] { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
        public static readonly byte[] Null = new byte[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
    }
}
