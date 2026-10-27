namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Installs, inspects, and removes one harness's Claude-shaped hooks and their installation
/// records. Settings writes are abandoned when a hash comparison immediately before writing
/// detects a change since the initial read. Install and status fail without writing when the
/// settings file, or another settings file whose hooks run in the same sessions, holds entries
/// of a conflicting template.
/// </summary>
internal interface IClaudeHooksInstallerService
{
    Task<ClaudeHooksInstallReport> InstallAsync(string scope, CancellationToken cancellationToken);

    Task<ClaudeHooksStatusReport> StatusAsync(string scope, CancellationToken cancellationToken);

    Task<ClaudeHooksUninstallReport> UninstallAsync(string scope, CancellationToken cancellationToken);
}
