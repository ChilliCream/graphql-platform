namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// One id that exists in more than one storage scope. Cross-scope duplicate
/// ids are invalid data: ids are collision-resistant, so the same id in two
/// scopes means the store was tampered with or corrupted outside the CLI.
/// </summary>
internal sealed record MemoryScopeConflict(
    string Id,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> Paths);
