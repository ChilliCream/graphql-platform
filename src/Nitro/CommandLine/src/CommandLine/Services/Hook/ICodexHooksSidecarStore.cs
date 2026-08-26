namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Reads and writes the Codex hooks sidecar file under the global config
/// directory. Read failures (missing file, corrupt JSON) resolve to
/// <see cref="CodexHooksSidecarFile.Empty"/> rather than throwing, same
/// degrade-not-fail contract as <see cref="ClaudeHooksSidecarStore"/>.
/// </summary>
internal interface ICodexHooksSidecarStore
{
    Task<CodexHooksSidecarFile> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(CodexHooksSidecarFile file, CancellationToken cancellationToken);
}
