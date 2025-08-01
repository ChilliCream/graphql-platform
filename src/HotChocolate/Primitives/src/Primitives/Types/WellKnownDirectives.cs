namespace HotChocolate.Types;

/// <summary>
/// Provides well-known directive names.
/// </summary>
public static class DirectiveNames
{
    /// <summary>
    /// The name constants of the @skip directive.
    /// </summary>
    public static class Skip
    {
        /// <summary>
        /// The name of the @skip directive.
        /// </summary>
        public const string Name = "skip";

        /// <summary>
        /// The argument names of the @skip directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the @skip if argument.
            /// </summary>
            public const string If = "if";
        }
    }

    /// <summary>
    /// The name constants of the @include directive.
    /// </summary>
    public static class Include
    {
        /// <summary>
        /// The name of the @include directive.
        /// </summary>
        public const string Name = "include";

        /// <summary>
        /// The argument names of the @include directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the @include if argument.
            /// </summary>
            public const string If = "if";
        }
    }

    /// <summary>
    /// The name constants of the @defer directive.
    /// </summary>
    public static class Defer
    {
        /// <summary>
        /// The name of the @defer directive.
        /// </summary>
        public const string Name = "defer";

        /// <summary>
        /// The argument names of the @defer directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the @defer label argument.
            /// </summary>
            public const string Label = "label";

            /// <summary>
            /// The name of the @defer if argument.
            /// </summary>
            public const string If = "if";
        }
    }

    /// <summary>
    /// The name constants of the @stream directive.
    /// </summary>
    public static class Stream
    {
        /// <summary>
        /// The name of the @stream directive.
        /// </summary>
        public const string Name = "stream";

        /// <summary>
        /// The argument names of the @stream directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the @stream label argument.
            /// </summary>
            public const string Label = "label";

            /// <summary>
            /// The name of the @stream initialCount argument.
            /// </summary>
            public const string InitialCount = "initialCount";

            /// <summary>
            /// The name of the @stream if argument.
            /// </summary>
            public const string If = "if";
        }
    }

    /// <summary>
    /// The name constants of the @oneOf directive.
    /// </summary>
    public static class OneOf
    {
        /// <summary>
        /// The name of the @oneOf directive.
        /// </summary>
        public const string Name = "oneOf";
    }

    public static class Deprecated
    {
        /// <summary>
        /// The name of the @deprecated directive.
        /// </summary>
        public const string Name = "deprecated";

        /// <summary>
        /// The argument names of the @deprecated directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the @deprecated reason argument.
            /// </summary>
            public const string Reason = "reason";

            /// <summary>
            /// The default reason of the @deprecated directive.
            /// </summary>
            public const string DefaultReason = "No longer supported.";
        }
    }

    /// <summary>
    /// The name constants of the @tag directive.
    /// </summary>
    public static class Tag
    {
        /// <summary>
        /// The name of the @tag directive.
        /// </summary>
        public const string Name = "tag";

        /// <summary>
        /// The argument names of the @tag directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the @tag name argument.
            /// </summary>
            public const string Name = "name";
        }
    }

    /// <summary>
    /// The name constants of the @semanticNonNull directive.
    /// </summary>
    public static class SemanticNonNull
    {
        /// <summary>
        /// The name of the @semanticNonNull directive.
        /// </summary>
        public const string Name = "semanticNonNull";

        /// <summary>
        /// The name of the @semanticNonNull argument levels.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the @semanticNonNull argument levels.
            /// </summary>
            public const string Levels = "levels";
        }
    }

    /// <summary>
    /// The name constants of the @specifiedBy directive.
    /// </summary>
    public static class SpecifiedBy
    {
        /// <summary>
        /// The name of the @specifiedBy directive.
        /// </summary>
        public const string Name = "specifiedBy";

        /// <summary>
        /// The argument names of the @specifiedBy directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the @specifiedBy url argument.
            /// </summary>
            public const string Url = "url";
        }
    }

    /// <summary>
    /// The name constants of the @lookup directive.
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--lookup"/>
    /// </summary>
    public static class Lookup
    {
        /// <summary>
        /// The name of the @lookup directive.
        /// </summary>
        public const string Name = "lookup";
    }

    /// <summary>
    /// The name constants of the @is directive.
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--is"/>
    /// </summary>
    public static class Is
    {
        /// <summary>
        /// The name of the @is directive.
        /// </summary>
        public const string Name = "is";
    }

    /// <summary>
    /// The name constants of the @internal directive.
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--internal"/>
    /// </summary>
    public static class Internal
    {
        /// <summary>
        /// The name of the @internal directive.
        /// </summary>
        public const string Name = "internal";
    }

    /// <summary>
    /// The name constants of the @inaccessible directive.
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </summary>
    public static class Inaccessible
    {
        /// <summary>
        /// The name of the @inaccessible directive.
        /// </summary>
        public const string Name = "inaccessible";
    }

    /// <summary>
    /// The name constants of the @key directive.
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--key"/>
    /// </summary>
    public static class Key
    {
        /// <summary>
        /// The name of the @key directive.
        /// </summary>
        public const string Name = "key";

        /// <summary>
        /// The argument names of the @key directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the @key fields argument.
            /// </summary>
            public const string Fields = "fields";
        }
    }

    /// <summary>
    /// The name constants of the @shareable directive.
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--shareable"/>
    /// </summary>
    public static class Shareable
    {
        /// <summary>
        /// The name of the @shareable directive.
        /// </summary>
        public const string Name = "shareable";
    }

    /// <summary>
    /// The name constants of the @provides directive.
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--provides"/>
    /// </summary>
    public static class Provides
    {
        /// <summary>
        /// The name of the @provides directive.
        /// </summary>
        public const string Name = "provides";
    }

    /// <summary>
    /// The name constants of the @require directive.
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--require"/>
    /// </summary>
    public static class Require
    {
        /// <summary>
        /// The name of the @require directive.
        /// </summary>
        public const string Name = "require";

        /// <summary>
        /// The argument names of the @require directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the @require field argument.
            /// </summary>
            public const string Field = "field";
        }
    }
}
