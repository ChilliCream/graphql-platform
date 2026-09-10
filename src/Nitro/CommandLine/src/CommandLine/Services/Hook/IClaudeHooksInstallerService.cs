namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Wires <see cref="ClaudeHooksEditor"/>'s pure JSON editing to the real
/// settings file, the sidecar, and the concurrency guard: the file is
/// re-read and hash-compared immediately before every write, so a foreign
/// edit landing between this service's initial read and its write aborts
/// the command instead of being silently overwritten.
/// </summary>
internal interface IClaudeHooksInstallerService
{
    Task<ClaudeHooksInstallReport> InstallAsync(string scope, CancellationToken cancellationToken);

    Task<ClaudeHooksStatusReport> StatusAsync(string scope, CancellationToken cancellationToken);

    Task<ClaudeHooksUninstallReport> UninstallAsync(string scope, CancellationToken cancellationToken);
}
