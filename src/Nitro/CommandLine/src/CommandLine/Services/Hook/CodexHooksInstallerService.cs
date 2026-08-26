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

        var sidecar = await sidecarStore.ReadAsync(cancellationToken);

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
        await sidecarStore.WriteAsync(sidecar, cancellationToken);

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

        var sidecar = await sidecarStore.ReadAsync(cancellationToken);

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
        await sidecarStore.WriteAsync(sidecar, cancellationToken);

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
    /// Same concurrency guard as
    /// <c>ClaudeHooksInstallerService.WriteIfUnchangedSinceReadAsync</c>:
    /// re-reads immediately before writing and aborts on a hash mismatch
    /// instead of clobbering a concurrent edit. Writes nothing when the new
    /// content is identical, so a no-op install/uninstall never touches the
    /// file's mtime.
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

    private static string Hash(string? text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty)));
}
