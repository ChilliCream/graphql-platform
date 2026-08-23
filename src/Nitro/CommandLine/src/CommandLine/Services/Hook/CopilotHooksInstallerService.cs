using System.Security.Cryptography;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record CopilotHooksInstallReport(
    string HooksJsonPath, IReadOnlyList<HookInstallEventResult> HooksEvents);

internal sealed record CopilotHooksStatusReport(
    string HooksJsonPath, IReadOnlyList<HookStatusEventResult> HooksEvents);

internal sealed record CopilotHooksUninstallReport(
    string HooksJsonPath, IReadOnlyList<HookUninstallEventResult> HooksEvents);

/// <summary>
/// Wires <see cref="CopilotHooksEditor"/>'s pure text editing to the real
/// hooks-dir file, the sidecar, and the same re-read-and-hash-compare
/// concurrency guard <see cref="ClaudeHooksInstallerService"/>/<see cref="CodexHooksInstallerService"/>
/// use. Simpler than the Codex installer: Copilot's user-scope surface is a
/// single, Nitro-owned file (<see cref="ICopilotPathResolver"/>), no
/// second config surface to wrap or restore.
/// </summary>
internal interface ICopilotHooksInstallerService
{
    Task<CopilotHooksInstallReport> InstallAsync(CancellationToken cancellationToken);

    Task<CopilotHooksStatusReport> StatusAsync(CancellationToken cancellationToken);

    Task<CopilotHooksUninstallReport> UninstallAsync(CancellationToken cancellationToken);
}

internal sealed class CopilotHooksInstallerService(
    IFileSystem fileSystem,
    ICopilotPathResolver pathResolver,
    ILaunchDescriptorResolver launchDescriptorResolver,
    ICopilotHooksSidecarStore sidecarStore,
    TimeProvider timeProvider) : ICopilotHooksInstallerService
{
    public async Task<CopilotHooksInstallReport> InstallAsync(CancellationToken cancellationToken)
    {
        var hooksJsonPath = pathResolver.ResolveHooksFile();
        var descriptor = launchDescriptorResolver.Resolve();

        var sidecar = await sidecarStore.ReadAsync(cancellationToken);

        var (textAtRead, hashAtRead) = await ReadWithHashAsync(hooksJsonPath, cancellationToken);
        var result = CopilotHooksEditor.Install(textAtRead, descriptor, timeProvider.GetUtcNow());
        await WriteIfUnchangedSinceReadAsync(hooksJsonPath, hashAtRead, result.HooksJson, cancellationToken);

        sidecar.HooksFiles[hooksJsonPath] = new Dictionary<string, CopilotHooksSidecarEntry>(result.Sidecar);
        await sidecarStore.WriteAsync(sidecar, cancellationToken);

        return new CopilotHooksInstallReport(hooksJsonPath, result.Outcomes);
    }

    public async Task<CopilotHooksStatusReport> StatusAsync(CancellationToken cancellationToken)
    {
        var hooksJsonPath = pathResolver.ResolveHooksFile();
        var descriptor = launchDescriptorResolver.Resolve();

        var text = fileSystem.FileExists(hooksJsonPath)
            ? await fileSystem.ReadAllTextAsync(hooksJsonPath, cancellationToken)
            : null;
        var events = CopilotHooksEditor.Status(text, descriptor);

        return new CopilotHooksStatusReport(hooksJsonPath, events);
    }

    public async Task<CopilotHooksUninstallReport> UninstallAsync(CancellationToken cancellationToken)
    {
        var hooksJsonPath = pathResolver.ResolveHooksFile();

        var (textAtRead, hashAtRead) = await ReadWithHashAsync(hooksJsonPath, cancellationToken);

        if (textAtRead is null)
        {
            // Nothing was ever installed: unlike a shared config file
            // (Claude's settings.json, Codex's hooks.json/config.toml),
            // this file is entirely Nitro-owned, so there is no reason to
            // create it - and its parent "hooks" directory - just to record
            // an empty result.
            return new CopilotHooksUninstallReport(
                hooksJsonPath,
                [.. CopilotHooksTemplate.Events.Select(e => new HookUninstallEventResult(e, HookUninstallOutcome.NotPresent))]);
        }

        var sidecar = await sidecarStore.ReadAsync(cancellationToken);
        var priorEntries = sidecar.HooksEntriesFor(hooksJsonPath);
        var result = CopilotHooksEditor.Uninstall(textAtRead, priorEntries);
        await WriteIfUnchangedSinceReadAsync(hooksJsonPath, hashAtRead, result.HooksJson, cancellationToken);

        sidecar.HooksFiles.Remove(hooksJsonPath);
        await sidecarStore.WriteAsync(sidecar, cancellationToken);

        return new CopilotHooksUninstallReport(hooksJsonPath, result.Outcomes);
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
