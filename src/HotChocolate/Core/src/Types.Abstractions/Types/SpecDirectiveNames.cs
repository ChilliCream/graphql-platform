namespace HotChocolate.Types;

public static class SpecDirectiveNames
{
    public static class Include
    {
        public const string Name = "include";

        public static class Arguments
        {
            public const string If = "if";
        }
    }

    public static class Skip
    {
        public const string Name = "skip";

        public static class Arguments
        {
            public const string If = "if";
        }
    }

    public static class Deprecated
    {
        public const string Name = "deprecated";

        public static class Arguments
        {
            public const string Reason = "reason";
        }
    }

    public static class SpecifiedBy
    {
        public const string Name = "specifiedBy";

        public static class Arguments
        {
            public const string Url = "url";
        }
    }

    public static bool IsSpecDirective(string name)
        => name switch
        {
            Include.Name => true,
            Skip.Name => true,
            Deprecated.Name => true,
            SpecifiedBy.Name => true,
            _ => false
        };
}
