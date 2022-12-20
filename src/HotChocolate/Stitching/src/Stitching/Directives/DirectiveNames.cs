namespace HotChocolate.Stitching;

internal static class DirectiveNames
{
    public static string Delegate { get; } = "delegate";

    public static string Computed { get; } = "computed";

    public static string Source { get; } = "source";

    public const string RemoveRootTypes = "_removeRootTypes";

    public const string RemoveType = "_removeType";

    public const string RenameType = "_renameType";

    public const string RenameField = "_renameField";
}
