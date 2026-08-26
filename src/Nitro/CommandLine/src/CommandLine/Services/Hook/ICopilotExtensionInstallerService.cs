namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Installs the Copilot CLI extension asset to
/// <c>&lt;repo-root&gt;/.github/extensions/nitro-mail/extension.mjs</c>,
/// project scope only. Unlike the hooks installers, which merge Nitro-owned
/// entries into a config file, the whole asset file is Nitro-owned, and the
/// safety question is not "did a foreign edit land since we last read this
/// file" but "is the file currently on disk something we recognize at all" -
/// content not matching any known asset version is refused unless
/// <c>--force</c>, per the plan's "overwrite refused if the on-disk hash
/// matches no known asset version" rule.
/// </summary>
internal interface ICopilotExtensionInstallerService
{
    Task<CopilotExtensionInstallReport> InstallAsync(bool force, CancellationToken cancellationToken);

    Task<CopilotExtensionStatusReport> StatusAsync(CancellationToken cancellationToken);

    Task<CopilotExtensionUninstallReport> UninstallAsync(CancellationToken cancellationToken);
}
