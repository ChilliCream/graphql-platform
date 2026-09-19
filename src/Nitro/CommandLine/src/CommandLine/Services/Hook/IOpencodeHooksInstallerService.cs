namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal interface IOpencodeHooksInstallerService
{
    Task<OpencodeHooksInstallReport> InstallAsync(string scope, CancellationToken cancellationToken);

    Task<OpencodeHooksStatusReport> StatusAsync(string scope, CancellationToken cancellationToken);

    Task<OpencodeHooksUninstallReport> UninstallAsync(string scope, CancellationToken cancellationToken);
}
