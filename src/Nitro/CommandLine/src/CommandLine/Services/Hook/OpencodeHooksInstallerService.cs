using System.Security.Cryptography;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class OpencodeHooksInstallerService(
    IFileSystem fileSystem,
    IOpencodePathResolver pathResolver,
    ILaunchDescriptorResolver launchDescriptorResolver,
    IOpencodeHooksSidecarStore sidecarStore,
    TimeProvider timeProvider) : IOpencodeHooksInstallerService
{
    public async Task<OpencodeHooksInstallReport> InstallAsync(string scope, CancellationToken cancellationToken)
    {
        var path = pathResolver.Resolve(scope);
        var descriptor = launchDescriptorResolver.Resolve();
        var template = OpencodeHooksTemplate.Build(descriptor);
        var (textAtRead, hashAtRead) = await ReadWithHashAsync(path, cancellationToken);

        if (textAtRead is not null && !IsOwned(textAtRead))
        {
            throw new ExitException($"'{path}' is not a Nitro-managed Opencode hooks file; nothing was written.");
        }

        var outcome = textAtRead switch
        {
            null => HookInstallOutcome.Installed,
            _ when textAtRead == template => HookInstallOutcome.Unchanged,
            _ => HookInstallOutcome.Updated
        };

        await WriteIfUnchangedSinceReadAsync(path, hashAtRead, template, cancellationToken);
        await UpdateSidecarAsync(
            file => file.Files[path] = new OpencodeHooksSidecarEntry(
                descriptor.BuildCommandLine([]), Hash(template), timeProvider.GetUtcNow()),
            cancellationToken);

        return new OpencodeHooksInstallReport(path, outcome);
    }

    public async Task<OpencodeHooksStatusReport> StatusAsync(string scope, CancellationToken cancellationToken)
    {
        var path = pathResolver.Resolve(scope);
        var template = OpencodeHooksTemplate.Build(launchDescriptorResolver.Resolve());
        var text = fileSystem.FileExists(path) ? await fileSystem.ReadAllTextAsync(path, cancellationToken) : null;

        var outcome = text switch
        {
            null => HookStatusOutcome.Missing,
            _ when !IsOwned(text) => HookStatusOutcome.Missing,
            _ when Hash(text) == Hash(template) => HookStatusOutcome.Installed,
            _ => HookStatusOutcome.Outdated
        };

        return new OpencodeHooksStatusReport(path, outcome);
    }

    public async Task<OpencodeHooksUninstallReport> UninstallAsync(string scope, CancellationToken cancellationToken)
    {
        var path = pathResolver.Resolve(scope);
        var (textAtRead, hashAtRead) = await ReadWithHashAsync(path, cancellationToken);
        var (sidecar, _) = await sidecarStore.ReadWithHashAsync(cancellationToken);
        var entry = sidecar.Files.GetValueOrDefault(path);
        var remove = textAtRead is not null
            && (entry?.ContentHash == Hash(textAtRead) || IsOwned(textAtRead));

        if (remove)
        {
            await DeleteIfUnchangedSinceReadAsync(path, hashAtRead, cancellationToken);
        }

        await UpdateSidecarAsync(file => file.Files.Remove(path), cancellationToken);

        return new OpencodeHooksUninstallReport(
            path,
            remove ? HookUninstallOutcome.Removed : HookUninstallOutcome.NotPresent);
    }

    private async Task<(string? Text, string Hash)> ReadWithHashAsync(string path, CancellationToken cancellationToken)
    {
        var text = fileSystem.FileExists(path) ? await fileSystem.ReadAllTextAsync(path, cancellationToken) : null;

        return (text, Hash(text));
    }

    private async Task WriteIfUnchangedSinceReadAsync(
        string path,
        string hashAtRead,
        string newText,
        CancellationToken cancellationToken)
    {
        var currentText = fileSystem.FileExists(path)
            ? await fileSystem.ReadAllTextAsync(path, cancellationToken)
            : null;

        if (Hash(currentText) != hashAtRead)
        {
            throw ChangedSinceRead(path);
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

    private async Task DeleteIfUnchangedSinceReadAsync(
        string path,
        string hashAtRead,
        CancellationToken cancellationToken)
    {
        var currentText = fileSystem.FileExists(path)
            ? await fileSystem.ReadAllTextAsync(path, cancellationToken)
            : null;

        if (Hash(currentText) != hashAtRead)
        {
            throw ChangedSinceRead(path);
        }

        if (currentText is not null)
        {
            fileSystem.DeleteFile(path);
        }
    }

    private async Task UpdateSidecarAsync(
        Action<OpencodeHooksSidecarFile> mutate,
        CancellationToken cancellationToken)
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
            "The Opencode hooks file was updated, but opencode-hooks-sidecar.json kept changing concurrently. "
            + "Re-run the command to repair the record.");
    }

    private static bool IsOwned(string text)
        => text.Contains(OpencodeHooksTemplate.OwnershipMarker, StringComparison.Ordinal);

    private static string Hash(string? text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty))).ToLowerInvariant();

    private static ExitException ChangedSinceRead(string path)
        => new($"'{path}' changed since it was read; nothing was written. Re-run the command.");
}
