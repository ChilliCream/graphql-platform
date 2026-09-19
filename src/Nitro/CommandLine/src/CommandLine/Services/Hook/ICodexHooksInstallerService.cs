namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Wires <see cref="CodexHooksEditor"/> and <see cref="CodexConfigTomlNotifyEditor"/>'s
/// pure text editing to the real <c>hooks.json</c> and <c>config.toml</c> files and
/// the notify-restoration sidecar, with a re-read-and-hash-compare concurrency
/// guard applied independently to each file.
/// </summary>
internal interface ICodexHooksInstallerService
{
    Task<CodexHooksInstallReport> InstallAsync(CancellationToken cancellationToken);

    Task<CodexHooksStatusReport> StatusAsync(CancellationToken cancellationToken);

    Task<CodexHooksUninstallReport> UninstallAsync(CancellationToken cancellationToken);
}
