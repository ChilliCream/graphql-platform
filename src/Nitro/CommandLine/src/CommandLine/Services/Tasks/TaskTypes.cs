namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The well-known task types. Custom lowercase types are accepted and
/// round-tripped verbatim.
/// </summary>
internal static class TaskTypes
{
    public const string Task = "task";
    public const string Bug = "bug";
    public const string Feature = "feature";
    public const string Epic = "epic";
    public const string Chore = "chore";
    public const string Docs = "docs";
    public const string Question = "question";

    public static string Normalize(string type)
        => type.Trim().ToLowerInvariant();
}
