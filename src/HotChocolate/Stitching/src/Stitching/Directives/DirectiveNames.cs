namespace HotChocolate.Stitching
{
    internal static class DirectiveNames
    {
        public static NameString Delegate { get; } = "delegate";

        public static NameString Computed { get; } = "computed";

        public static NameString Source { get; } = "source";

        public const string RemoveRootTypes = "_removeRootTypes";

        public const string RemoveType = "_removeType";

        public const string RenameType = "_renameType";

        public const string RenameField = "_renameField";
    }
}
