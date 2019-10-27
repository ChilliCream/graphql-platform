namespace HotChocolate.Language
{
    internal static class Keywords
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
        public const string Directive = "directive";
        public const string Implements = "implements";
        public const string Repeatable = "repeatable";

        // query
        public const string Query = "query";
        public const string Mutation = "mutation";
        public const string Subscription = "subscription";
        public const string Fragment = "fragment";

        // general
        public const string On = "on";
        public const string True = "true";
        public const string False = "false";
        public const string Null = "null";
    }
}
