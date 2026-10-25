namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Reads and writes the Claude hooks sidecar in the global configuration directory.
/// A missing file, JSON null, or invalid JSON yields <see cref="ClaudeHooksSidecarFile.Empty"/>.
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
    /// Writes the sidecar when its current text hash matches <paramref name="hashAtRead"/>.
    /// Returns false without writing when the hashes differ.
    /// </summary>
    Task<bool> WriteIfUnchangedAsync(ClaudeHooksSidecarFile file, string hashAtRead, CancellationToken cancellationToken);
}
