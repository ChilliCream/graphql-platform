namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Wires <see cref="CodexHooksEditor"/> and
/// <see cref="CodexConfigTomlNotifyEditor"/>'s pure text editing to the real
/// <c>hooks.json</c> and <c>config.toml</c> files, the notify-restoration sidecar, and the same
/// re-read-and-hash-compare concurrency guard
/// <see cref="ClaudeHooksInstallerService"/> uses - applied independently to
/// EACH of the two files, so a foreign edit racing one of them aborts only
/// that file's write.
/// </summary>
internal interface ICodexHooksInstallerService
{
    Task<CodexHooksInstallReport> InstallAsync(CancellationToken cancellationToken);

    Task<CodexHooksStatusReport> StatusAsync(CancellationToken cancellationToken);

    Task<CodexHooksUninstallReport> UninstallAsync(CancellationToken cancellationToken);
}
