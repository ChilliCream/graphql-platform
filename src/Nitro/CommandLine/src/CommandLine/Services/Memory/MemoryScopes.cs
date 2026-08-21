namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The memory storage scopes: the project-local store and the machine-local
/// global store.
/// </summary>
internal static class MemoryScopes
{
    public const string Project = "project";
    public const string Global = "global";

    /// <summary>
    /// Not a storage scope: a read-time filter value meaning the union of
    /// <see cref="Project"/> and <see cref="Global"/>, project band first.
    /// </summary>
    public const string All = "all";

    public static string Normalize(string scope) => scope.Trim().ToLowerInvariant();

    /// <summary>
    /// True for an actual storage scope a memory can live in. <see cref="All"/>
    /// is a read filter, not a storage scope, and is not valid here.
    /// </summary>
    public static bool IsValid(string normalizedScope) => normalizedScope is Project or Global;

    /// <summary>
    /// True for a value a read command's <c>--scope</c> option accepts:
    /// <see cref="Project"/>, <see cref="Global"/>, or <see cref="All"/>.
    /// </summary>
    public static bool IsValidReadScope(string normalizedScope)
        => normalizedScope is Project or Global or All;
}
