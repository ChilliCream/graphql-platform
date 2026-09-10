namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The well-known dependency types. Custom lowercase kebab-case types are
/// accepted and round-tripped verbatim.
/// </summary>
internal static class TaskDependencyTypes
{
    public const string Blocks = "blocks";
    public const string ParentChild = "parent-child";
    public const string ConditionalBlocks = "conditional-blocks";
    public const string WaitsFor = "waits-for";
    public const string Related = "related";
    public const string DiscoveredFrom = "discovered-from";
    public const string RelatesTo = "relates-to";
    public const string Duplicates = "duplicates";
    public const string Supersedes = "supersedes";
    public const string CausedBy = "caused-by";

    /// <summary>
    /// A blocking dependency gates readiness of the dependent task until the
    /// target task reaches a terminal state.
    /// </summary>
    public static bool IsBlocking(string type)
        => type is Blocks or ParentChild or ConditionalBlocks or WaitsFor;

    public static string Normalize(string type)
        => type.Trim().ToLowerInvariant().Replace('_', '-');
}
