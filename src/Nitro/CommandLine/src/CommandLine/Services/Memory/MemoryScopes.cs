namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The memory storage scopes: the project-local store and the machine-local
/// global store.
/// </summary>
internal static class MemoryScopes
{
    public const string Project = "project";
    public const string Global = "global";

    public static string Normalize(string scope) => scope.Trim().ToLowerInvariant();

    public static bool IsValid(string normalizedScope) => normalizedScope is Project or Global;
}
