namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Thrown by an unscoped (<c>all</c>) merged read that encounters one or
/// more ids present in both the project and global stores. Callers must
/// catch this instead of letting it fall through to the generic exception
/// handler, so the command can emit a structured diagnostic and a nonzero
/// exit code with no partial memory result, per the invalid-data contract
/// for cross-scope duplicate ids.
/// </summary>
internal sealed class MemoryScopeConflictException(IReadOnlyList<MemoryScopeConflict> conflicts) : Exception
{
    public IReadOnlyList<MemoryScopeConflict> Conflicts { get; } = conflicts;
}
