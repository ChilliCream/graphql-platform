using System.Security.Cryptography;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class ClaudeHooksInstallerService(
    IFileSystem fileSystem,
    IClaudeSettingsPathResolver pathResolver,
    ILaunchDescriptorResolver launchDescriptorResolver,
    IClaudeHooksSidecarStore sidecarStore,
    TimeProvider timeProvider) : IClaudeHooksInstallerService
{
    public async Task<ClaudeHooksInstallReport> InstallAsync(string scope, CancellationToken cancellationToken)
    {
        var path = pathResolver.Resolve(scope);
        var descriptor = launchDescriptorResolver.Resolve();

        var (textAtRead, hashAtRead) = await ReadWithHashAsync(path, cancellationToken);

        var result = ClaudeHooksEditor.Install(textAtRead, descriptor, timeProvider.GetUtcNow());

        await WriteIfUnchangedSinceReadAsync(path, hashAtRead, result.SettingsJson, cancellationToken);

        await UpdateSidecarAsync(
            s => s.Files[path] = new Dictionary<string, ClaudeHooksSidecarEntry>(result.Sidecar),
            cancellationToken);

        return new ClaudeHooksInstallReport(path, result.Outcomes);
    }

    public async Task<ClaudeHooksStatusReport> StatusAsync(string scope, CancellationToken cancellationToken)
    {
        var path = pathResolver.Resolve(scope);
        var descriptor = launchDescriptorResolver.Resolve();

        var text = fileSystem.FileExists(path) ? await fileSystem.ReadAllTextAsync(path, cancellationToken) : null;

        return new ClaudeHooksStatusReport(path, ClaudeHooksEditor.Status(text, descriptor));
    }

    public async Task<ClaudeHooksUninstallReport> UninstallAsync(string scope, CancellationToken cancellationToken)
    {
        var path = pathResolver.Resolve(scope);

        var (textAtRead, hashAtRead) = await ReadWithHashAsync(path, cancellationToken);

        var priorEntries = (await sidecarStore.ReadAsync(cancellationToken)).EntriesFor(path);

        var result = ClaudeHooksEditor.Uninstall(textAtRead, priorEntries);

        await WriteIfUnchangedSinceReadAsync(path, hashAtRead, result.SettingsJson, cancellationToken);

        await UpdateSidecarAsync(s => s.Files.Remove(path), cancellationToken);

        return new ClaudeHooksUninstallReport(path, result.Outcomes);
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

    private static string Hash(string? text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty)));

    /// <summary>
    /// Applies <paramref name="mutate"/> and writes the sidecar, retrying reported hash
    /// mismatches up to five times before throwing <see cref="ExitException"/>.
    /// </summary>
    private async Task UpdateSidecarAsync(
        Action<ClaudeHooksSidecarFile> mutate, CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var (file, hash) = await sidecarStore.ReadWithHashAsync(cancellationToken);
            mutate(file);

            if (await sidecarStore.WriteIfUnchangedAsync(file, hash, cancellationToken))
            {
                return;
            }
        }

        throw new ExitException(
            "The settings file was updated, but the claude-hooks-sidecar.json record could not be "
            + "written because it kept changing concurrently. Re-run the command to repair the record.");
    }
}
