namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Installs, inspects, and removes Claude hooks and their installation records.
/// Settings writes are abandoned when a hash comparison immediately before writing
/// detects a change since the initial read.
/// </summary>
internal interface IClaudeHooksInstallerService
{
    Task<ClaudeHooksInstallReport> InstallAsync(string scope, CancellationToken cancellationToken);

    Task<ClaudeHooksStatusReport> StatusAsync(string scope, CancellationToken cancellationToken);

    Task<ClaudeHooksUninstallReport> UninstallAsync(string scope, CancellationToken cancellationToken);
}
