using System.Security.Cryptography;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexHooksInstallerService(
    IFileSystem fileSystem,
    ICodexPathResolver pathResolver,
    ILaunchDescriptorResolver launchDescriptorResolver,
    ICodexHooksSidecarStore sidecarStore,
    TimeProvider timeProvider) : ICodexHooksInstallerService
{
    public async Task<CodexHooksInstallReport> InstallAsync(CancellationToken cancellationToken)
    {
        var hooksJsonPath = pathResolver.ResolveHooksJson();
        var configTomlPath = pathResolver.ResolveConfigToml();
        var descriptor = launchDescriptorResolver.Resolve();

        var (sidecar, sidecarHashAtRead) = await sidecarStore.ReadWithHashAsync(cancellationToken);

        var (hooksTextAtRead, hooksHashAtRead) = await ReadWithHashAsync(hooksJsonPath, cancellationToken);
        var hooksResult = CodexHooksEditor.Install(hooksTextAtRead, descriptor);
        await WriteIfUnchangedSinceReadAsync(hooksJsonPath, hooksHashAtRead, hooksResult.HooksJson, cancellationToken);

        var ourArgv = CodexNotifyTemplate.BuildArgv(descriptor);
        var existingNotify = sidecar.NotifyEntryFor(configTomlPath);
        var (tomlTextAtRead, tomlHashAtRead) = await ReadWithHashAsync(configTomlPath, cancellationToken);
        var notifyResult = CodexConfigTomlNotifyEditor.Install(
            tomlTextAtRead, ourArgv, existingNotify?.OurArgv, existingNotify?.PriorForeign);
        await WriteIfUnchangedSinceReadAsync(
            configTomlPath, tomlHashAtRead, notifyResult.ConfigToml, cancellationToken);

        sidecar.NotifyFiles[configTomlPath] = new CodexNotifySidecarEntry(
            ourArgv, notifyResult.NewPriorForeign, timeProvider.GetUtcNow());
        if (!await sidecarStore.WriteIfUnchangedAsync(sidecar, sidecarHashAtRead, cancellationToken))
        {
            throw SidecarChanged();
        }

        return new CodexHooksInstallReport(
            hooksJsonPath,
            hooksResult.Outcomes,
            configTomlPath,
            notifyResult.Outcome,
            notifyResult.NewPriorForeign is not null);
    }

    public async Task<CodexHooksStatusReport> StatusAsync(CancellationToken cancellationToken)
    {
        var hooksJsonPath = pathResolver.ResolveHooksJson();
        var configTomlPath = pathResolver.ResolveConfigToml();
        var descriptor = launchDescriptorResolver.Resolve();

        var hooksText = fileSystem.FileExists(hooksJsonPath)
            ? await fileSystem.ReadAllTextAsync(hooksJsonPath, cancellationToken)
            : null;
        var hooksEvents = CodexHooksEditor.Status(hooksText, descriptor);

        var tomlText = fileSystem.FileExists(configTomlPath)
            ? await fileSystem.ReadAllTextAsync(configTomlPath, cancellationToken)
            : null;
        var ourArgv = CodexNotifyTemplate.BuildArgv(descriptor);
        var notifyOutcome = CodexConfigTomlNotifyEditor.Status(tomlText, ourArgv);

        return new CodexHooksStatusReport(hooksJsonPath, hooksEvents, configTomlPath, notifyOutcome);
    }

    public async Task<CodexHooksUninstallReport> UninstallAsync(CancellationToken cancellationToken)
    {
        var hooksJsonPath = pathResolver.ResolveHooksJson();
        var configTomlPath = pathResolver.ResolveConfigToml();
        var descriptor = launchDescriptorResolver.Resolve();

        var (sidecar, sidecarHashAtRead) = await sidecarStore.ReadWithHashAsync(cancellationToken);

        var (hooksTextAtRead, hooksHashAtRead) = await ReadWithHashAsync(hooksJsonPath, cancellationToken);
        var hooksResult = CodexHooksEditor.Uninstall(hooksTextAtRead);
        await WriteIfUnchangedSinceReadAsync(hooksJsonPath, hooksHashAtRead, hooksResult.HooksJson, cancellationToken);

        var ourArgv = CodexNotifyTemplate.BuildArgv(descriptor);
        var notifyEntry = sidecar.NotifyEntryFor(configTomlPath);
        var (tomlTextAtRead, tomlHashAtRead) = await ReadWithHashAsync(configTomlPath, cancellationToken);
        var notifyResult = CodexConfigTomlNotifyEditor.Uninstall(
            tomlTextAtRead, notifyEntry?.OurArgv ?? ourArgv, notifyEntry?.PriorForeign);
        await WriteIfUnchangedSinceReadAsync(
            configTomlPath, tomlHashAtRead, notifyResult.ConfigToml, cancellationToken);

        sidecar.NotifyFiles.Remove(configTomlPath);
        if (!await sidecarStore.WriteIfUnchangedAsync(sidecar, sidecarHashAtRead, cancellationToken))
        {
            throw SidecarChanged();
        }

        return new CodexHooksUninstallReport(
            hooksJsonPath,
            hooksResult.Outcomes,
            configTomlPath,
            notifyResult.Outcome,
            notifyEntry?.PriorForeign is not null);
    }

    private async Task<(string? Text, string Hash)> ReadWithHashAsync(string path, CancellationToken cancellationToken)
    {
        var text = fileSystem.FileExists(path) ? await fileSystem.ReadAllTextAsync(path, cancellationToken) : null;

        return (text, Hash(text));
    }

    /// <summary>
    /// Writes changed content only when the destination hash, re-read immediately
    /// before writing, matches the hash captured by the caller.
    /// </summary>
    private async Task WriteIfUnchangedSinceReadAsync(
        string path, string hashAtRead, string newText, CancellationToken cancellationToken)
    {
        var currentText = fileSystem.FileExists(path)
            ? await fileSystem.ReadAllTextAsync(path, cancellationToken)
            : null;

        if (Hash(currentText) != hashAtRead)
        {
            throw new ExitException(
                $"'{path}' changed since it was read; nothing was written. Re-run the command.");
        }

        if (currentText == newText)
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }

        if (currentText is null)
        {
            await fileSystem.CreateFileAtomicAsync(path, newText, cancellationToken);
        }
        else
        {
            await fileSystem.ReplaceFileAtomicAsync(path, newText, cancellationToken);
        }
    }

    private static ExitException SidecarChanged()
        => new(
            "The Codex hook configuration was updated, but codex-hooks-sidecar.json changed concurrently. "
            + "Re-run the command to repair the record.");

    private static string Hash(string? text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty)));
}
