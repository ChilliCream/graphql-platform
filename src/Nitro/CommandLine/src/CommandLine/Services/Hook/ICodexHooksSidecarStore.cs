namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Reads and writes the Codex hooks sidecar in the global configuration directory.
/// A missing file, JSON null, or invalid JSON yields <see cref="CodexHooksSidecarFile.Empty"/>.
/// </summary>
internal interface ICodexHooksSidecarStore
{
    Task<CodexHooksSidecarFile> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(CodexHooksSidecarFile file, CancellationToken cancellationToken);
}
