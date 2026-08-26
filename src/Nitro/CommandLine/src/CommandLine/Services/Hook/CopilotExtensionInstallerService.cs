using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CopilotExtensionInstallerService(
    IFileSystem fileSystem,
    ICopilotExtensionPathResolver pathResolver,
    ILaunchDescriptorResolver launchDescriptorResolver,
    TimeProvider timeProvider) : ICopilotExtensionInstallerService
{
    public async Task<CopilotExtensionInstallReport> InstallAsync(bool force, CancellationToken cancellationToken)
    {
        var extensionPath = pathResolver.ResolveExtensionFile();
        var configPath = pathResolver.ResolveConfigFile();

        var existing = fileSystem.FileExists(extensionPath)
            ? await fileSystem.ReadAllTextAsync(extensionPath, cancellationToken)
            : null;

        var outcome = existing switch
        {
            null => CopilotExtensionInstallOutcome.Installed,
            var text when text == CopilotExtensionAsset.Content => CopilotExtensionInstallOutcome.Unchanged,
            var text when CopilotExtensionAsset.IsKnownVersion(text) => CopilotExtensionInstallOutcome.Updated,
            _ when force => CopilotExtensionInstallOutcome.Forced,
            _ => throw new ExitException(
                $"'{extensionPath}' already exists and its content does not match any known nitro-mail "
                + "extension asset version. Refusing to overwrite it (it may be a hand edit or an "
                + "unrelated file); pass --force to overwrite anyway.")
        };

        if (outcome != CopilotExtensionInstallOutcome.Unchanged)
        {
            await WriteExtensionFileAsync(extensionPath, existing is not null, cancellationToken);
        }

        await WriteConfigFileAsync(configPath, cancellationToken);

        return new CopilotExtensionInstallReport(extensionPath, configPath, outcome);
    }

    public async Task<CopilotExtensionStatusReport> StatusAsync(CancellationToken cancellationToken)
    {
        var extensionPath = pathResolver.ResolveExtensionFile();
        var configPath = pathResolver.ResolveConfigFile();

        if (!fileSystem.FileExists(extensionPath))
        {
            return new CopilotExtensionStatusReport(
                extensionPath, configPath, CopilotExtensionStatusOutcome.Missing);
        }

        var text = await fileSystem.ReadAllTextAsync(extensionPath, cancellationToken);

        var outcome = text == CopilotExtensionAsset.Content
            ? CopilotExtensionStatusOutcome.Current
            : CopilotExtensionAsset.IsKnownVersion(text)
                ? CopilotExtensionStatusOutcome.Outdated
                : CopilotExtensionStatusOutcome.Unrecognized;

        return new CopilotExtensionStatusReport(extensionPath, configPath, outcome);
    }

    public Task<CopilotExtensionUninstallReport> UninstallAsync(CancellationToken cancellationToken)
    {
        var extensionPath = pathResolver.ResolveExtensionFile();
        var configPath = pathResolver.ResolveConfigFile();

        var removed = false;

        if (fileSystem.FileExists(extensionPath))
        {
            fileSystem.DeleteFile(extensionPath);
            removed = true;
        }

        if (fileSystem.FileExists(configPath))
        {
            fileSystem.DeleteFile(configPath);
            removed = true;
        }

        return Task.FromResult(new CopilotExtensionUninstallReport(extensionPath, configPath, removed));
    }

    private async Task WriteExtensionFileAsync(string path, bool destinationExists, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }

        if (destinationExists)
        {
            await fileSystem.ReplaceFileAtomicAsync(path, CopilotExtensionAsset.Content, cancellationToken);
        }
        else
        {
            await fileSystem.CreateFileAtomicAsync(path, CopilotExtensionAsset.Content, cancellationToken);
        }
    }

    /// <summary>
    /// The launch descriptor (how this <c>nitro</c> was invoked) is
    /// machine-specific data, not part of the versioned asset, so unlike
    /// the asset file it is always rewritten to the current descriptor on
    /// every install, no hash comparison involved - the same rule the hooks
    /// installers apply to the command lines they embed.
    /// </summary>
    private async Task WriteConfigFileAsync(string path, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }

        var descriptor = launchDescriptorResolver.Resolve();

        var config = new CopilotExtensionConfig(
            descriptor.Executable,
            descriptor.ArgumentPrefix,
            CopilotExtensionAsset.CurrentVersion,
            timeProvider.GetUtcNow());

        var json = JsonSerializer.Serialize(config, CopilotExtensionConfigJsonContext.Default.CopilotExtensionConfig);

        if (fileSystem.FileExists(path))
        {
            await fileSystem.ReplaceFileAtomicAsync(path, json, cancellationToken);
        }
        else
        {
            await fileSystem.CreateFileAtomicAsync(path, json, cancellationToken);
        }
    }
}
