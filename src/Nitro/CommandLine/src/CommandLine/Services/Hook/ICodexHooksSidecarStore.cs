namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Reads and writes the Codex hooks sidecar in the global configuration directory.
/// A missing file, JSON null, or invalid JSON yields <see cref="CodexHooksSidecarFile.Empty"/>.
/// </summary>
internal interface ICodexHooksSidecarStore
{
    Task<CodexHooksSidecarFile> ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads the sidecar together with a hash of its underlying file text at read time.
    /// </summary>
    Task<(CodexHooksSidecarFile File, string Hash)> ReadWithHashAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Writes the sidecar when its current text hash matches <paramref name="hashAtRead"/>.
    /// Returns false without writing when the hashes differ.
    /// </summary>
    Task<bool> WriteIfUnchangedAsync(CodexHooksSidecarFile file, string hashAtRead, CancellationToken cancellationToken);
}
