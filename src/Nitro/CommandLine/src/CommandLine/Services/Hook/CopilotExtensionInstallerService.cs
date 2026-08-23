using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal enum CopilotExtensionInstallOutcome
{
    /// <summary>No file existed at the destination; it was created.</summary>
    Installed,

    /// <summary>
    /// A recognized prior asset version was on disk; it was replaced with
    /// the current version.
    /// </summary>
    Updated,

    /// <summary>The current asset version was already on disk; nothing was written.</summary>
    Unchanged,

    /// <summary>
    /// Content not matching any known asset version was on disk (a
    /// hand-edited or entirely foreign file) and <c>--force</c> was passed,
    /// so it was overwritten anyway.
    /// </summary>
    Forced
}

internal enum CopilotExtensionStatusOutcome
{
    Missing,
    Current,
    Outdated,

    /// <summary>
    /// On-disk content matches no asset version this CLI recognizes: not
    /// something <c>install</c> wrote, and not safe to overwrite without
    /// <c>--force</c>.
    /// </summary>
    Unrecognized
}

internal sealed record CopilotExtensionInstallReport(
    string ExtensionPath, string ConfigPath, CopilotExtensionInstallOutcome Outcome);

internal sealed record CopilotExtensionStatusReport(
    string ExtensionPath, string ConfigPath, CopilotExtensionStatusOutcome Outcome);

internal sealed record CopilotExtensionUninstallReport(string ExtensionPath, string ConfigPath, bool Removed);

/// <summary>
/// Installs the Copilot CLI extension asset (perles-net-k3j.16) to
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

/// <summary>
/// The <c>nitro-mail.config.json</c> content <c>extension.mjs</c> reads at
/// runtime to invoke <c>nitro</c>: the same executable-plus-argument-prefix
/// shape <see cref="LaunchDescriptor"/> carries, serialized so a Node.js
/// process (with no access to the .NET record) can read it.
/// </summary>
internal sealed record CopilotExtensionConfig(
    string Executable,
    IReadOnlyList<string> ArgumentPrefix,
    int ExtensionVersion,
    DateTimeOffset InstalledAt);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CopilotExtensionConfig))]
internal sealed partial class CopilotExtensionConfigJsonContext : JsonSerializerContext;
