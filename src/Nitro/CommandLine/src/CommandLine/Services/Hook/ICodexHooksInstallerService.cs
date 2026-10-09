namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Installs, inspects, and removes Codex hooks and notify configuration.
/// Configuration writes are abandoned when a hash comparison immediately before
/// writing detects a change since the initial read.
/// </summary>
internal interface ICodexHooksInstallerService
{
    Task<CodexHooksInstallReport> InstallAsync(CancellationToken cancellationToken);

    Task<CodexHooksStatusReport> StatusAsync(CancellationToken cancellationToken);

    Task<CodexHooksUninstallReport> UninstallAsync(CancellationToken cancellationToken);
}
