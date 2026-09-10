namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Reads and writes the Claude hooks sidecar file under the global config
/// directory. Read failures (missing file, corrupt JSON) resolve to
/// <see cref="ClaudeHooksSidecarFile.Empty"/> rather than throwing: a lost
/// or corrupted sidecar degrades install/uninstall to marker-based
/// detection, it does not fail the command.
/// </summary>
internal interface IClaudeHooksSidecarStore
{
    Task<ClaudeHooksSidecarFile> ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads the sidecar together with a hash of its underlying file text at
    /// read time, for passing to <see cref="WriteIfUnchangedAsync"/>.
    /// </summary>
    Task<(ClaudeHooksSidecarFile File, string Hash)> ReadWithHashAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The concurrency guard for the sidecar's read-modify-write cycle:
    /// re-reads the sidecar file immediately before writing and compares its
    /// hash against <paramref name="hashAtRead"/>, the one captured by the
    /// caller's earlier <see cref="ReadWithHashAsync"/>. A mismatch means a
    /// concurrent install or uninstall wrote to the sidecar in between; this
    /// reports the mismatch to the caller instead of writing, returning
    /// <see langword="false"/>.
    /// </summary>
    Task<bool> WriteIfUnchangedAsync(ClaudeHooksSidecarFile file, string hashAtRead, CancellationToken cancellationToken);
}
