namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The structured diagnostic returned in place of a memory result when an
/// unscoped merged read encounters one or more cross-scope duplicate ids.
/// </summary>
internal sealed record MemoryScopeConflictResult(IReadOnlyList<MemoryScopeConflict> Conflicts)
{
    public static MemoryScopeConflictResult Create(MemoryScopeConflictException exception) => new(exception.Conflicts);
}
