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
    /// The name constants of the @external directive.
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--external"/>
    /// </summary>
    public static class External
    {
        /// <summary>
        /// The name of the @external directive.
        /// </summary>
        public const string Name = "external";
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
    /// The name constants of the @override directive.
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--override"/>
    /// </summary>
    public static class Override
    {
        /// <summary>
        /// The name of the @override directive.
        /// </summary>
        public const string Name = "override";

        /// <summary>
        /// The argument names of the @override directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the from argument.
            /// </summary>
            public const string From = "from";
        }
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
            /// The name of the field argument.
            /// </summary>
            public const string Field = "field";
        }
    }

    /// <summary>
    /// The name constants of the @serializeAs directive.
    /// </summary>
    public static class SerializeAs
    {
        /// <summary>
        /// The name of the @serializeAs directive.
        /// </summary>
        public const string Name = "serializeAs";

        /// <summary>
        /// The argument names of the @serializeAs directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name the type argument.
            /// </summary>
            public const string Type = "type";

            /// <summary>
            /// The name of the pattern argument.
            /// </summary>
            public const string Pattern = "pattern";
        }
    }

    /// <summary>
    /// The name constants of the @cacheControl directive.
    /// </summary>
    public static class CacheControl
    {
        /// <summary>
        /// The name of the @cacheControl directive.
        /// </summary>
        public const string Name = "cacheControl";

        /// <summary>
        /// The argument names of the @cacheControl directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the inheritMaxAge argument.
            /// </summary>
            public const string InheritMaxAge = "inheritMaxAge";

            /// <summary>
            /// The name of the maxAge argument.
            /// </summary>
            public const string MaxAge = "maxAge";

            /// <summary>
            /// The name of the scope argument.
            /// </summary>
            public const string Scope = "scope";

            /// <summary>
            /// The name of the sharedMaxAge argument.
            /// </summary>
            public const string SharedMaxAge = "sharedMaxAge";

            /// <summary>
            /// The name of the vary argument.
            /// </summary>
            public const string Vary = "vary";
        }
    }

    /// <summary>
    /// The name constants of the @cost directive.
    /// </summary>
    public static class Cost
    {
        public const string Name = "cost";

        /// <summary>
        /// The argument names of the @cost directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the weight argument.
            /// </summary>
            public const string Weight = "weight";
        }
    }

    /// <summary>
    /// The name constants of the @listSize directive.
    /// </summary>
    public static class ListSize
    {
        public const string Name = "listSize";

        /// <summary>
        /// The argument names of the @listSize directive.
        /// </summary>
        public static class Arguments
        {
            /// <summary>
            /// The name of the assumedSize argument.
            /// </summary>
            public const string AssumedSize = "assumedSize";

            /// <summary>
            /// The name of the slicingArguments argument.
            /// </summary>
            public const string SlicingArguments = "slicingArguments";

            /// <summary>
            /// The name of the slicingArgumentDefaultValue argument.
            /// </summary>
            public const string SlicingArgumentDefaultValue = "slicingArgumentDefaultValue";

            /// <summary>
            /// The name of the sizedFields argument.
            /// </summary>
            public const string SizedFields = "sizedFields";

            /// <summary>
            /// The name of the requireOneSlicingArgument argument.
            /// </summary>
            public const string RequireOneSlicingArgument = "requireOneSlicingArgument";
        }
    }

    public static bool IsSpecDirective(string name)
        => name switch
        {
            Include.Name => true,
            Skip.Name => true,
            Deprecated.Name => true,
            SpecifiedBy.Name => true,
            OneOf.Name => true,
            _ => false
        };
}
