using HotChocolate;


namespace HotChocolate.Stitching.Delegation
{
    public static class ScopeNames
    {
        public static NameString Arguments { get; } = "arguments";
        public static NameString Fields { get; } = "fields";
        public static NameString ContextData { get; } = "contextData";
        public static NameString ScopedContextData { get; } =
            "scopedContextData";
    }
}
