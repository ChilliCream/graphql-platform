using System;

namespace StrawberryShake.VisualStudio.Language
{
    internal static class GraphQLKeywords
    {
        // type system
        public static ReadOnlySpan<char> Schema => "schema".AsSpan();
        public static ReadOnlySpan<char> Scalar => "scalar".AsSpan();
        public static ReadOnlySpan<char> Type => "type".AsSpan();
        public static ReadOnlySpan<char> Interface => "interface".AsSpan();
        public static ReadOnlySpan<char> Union => "union".AsSpan();
        public static ReadOnlySpan<char> Enum => "enum".AsSpan();
        public static ReadOnlySpan<char> Input => "input".AsSpan();
        public static ReadOnlySpan<char> Extend => "extend".AsSpan();
        public static ReadOnlySpan<char> Directive => "directive".AsSpan();
        public static ReadOnlySpan<char> Implements => "implements".AsSpan();
        public static ReadOnlySpan<char> Repeatable => "repeatable".AsSpan();

        // query
        public static ReadOnlySpan<char> Query => "query".AsSpan();
        public static ReadOnlySpan<char> Mutation => "mutation".AsSpan();
        public static ReadOnlySpan<char> Subscription => "subscription".AsSpan();
        public static ReadOnlySpan<char> Fragment => "fragment".AsSpan();

        // general
        public static ReadOnlySpan<char> On => "on".AsSpan();
        public static ReadOnlySpan<char> True => "true".AsSpan();
        public static ReadOnlySpan<char> False => "false".AsSpan();
        public static ReadOnlySpan<char> Null => "null".AsSpan();
    }
}
