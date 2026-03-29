#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Net;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.FusionConfiguration;
using HotChocolate.Fusion;
using HotChocolate.Fusion.SourceSchema.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionPublishCommand : Command
{
    public FusionPublishCommand(
        FusionConfigurationPublishBeginCommand fusionConfigurationPublishBeginCommand,
        FusionConfigurationPublishStartCommand fusionConfigurationPublishStartCommand,
        FusionConfigurationPublishValidateCommand fusionConfigurationPublishValidateCommand,
        FusionConfigurationPublishCancelCommand fusionConfigurationPublishCancelCommand,
        FusionConfigurationPublishCommitCommand fusionConfigurationPublishCommitCommand,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem)
        : base("publish")
    {
        Description = "Publishes a Fusion archive to Nitro."
            + Environment.NewLine
            + "To take control over the deployment orchestration use sub-commands like 'begin'."
            + Environment.NewLine
            + $"If you don't specify {FusionArchiveFileOption.OptionName} and instead use "
            + $"{OptionalSourceSchemaIdentifierListOption.OptionName} or {OptionalSourceSchemaFileListOption.OptionName}, "
            + "a Fusion v2 composition will be performed internally."
            + Environment.NewLine
            + "The orchestration sub-commands can be used for both Fusion v1 and v2.";

        Subcommands.Add(fusionConfigurationPublishBeginCommand);
        Subcommands.Add(fusionConfigurationPublishStartCommand);
        Subcommands.Add(fusionConfigurationPublishValidateCommand);
        Subcommands.Add(fusionConfigurationPublishCancelCommand);
        Subcommands.Add(fusionConfigurationPublishCommitCommand);

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<OptionalSourceSchemaIdentifierListOption>.Instance);
        Options.Add(Opt<OptionalSourceSchemaFileListOption>.Instance);
        Options.Add(Opt<OptionalFusionArchiveFileOption>.Instance);
        Options.Add(Opt<WorkingDirectoryOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);
        this.AddGlobalNitroOptions();

        Validators.Add(result =>
        {
            var exclusiveOptionsCount = new[]
            {
                result.GetValue(Opt<OptionalSourceSchemaFileListOption>.Instance) is { Count: > 0 },
                result.GetValue(Opt<OptionalSourceSchemaIdentifierListOption>.Instance) is { Count: > 0 },
                result.GetValue(Opt<OptionalFusionArchiveFileOption>.Instance) is not null
            }.Count(x => x);

            if (exclusiveOptionsCount > 1)
            {
                result.AddError(
                    $"You can only specify one of: '{OptionalSourceSchemaIdentifierListOption.OptionName}', "
                    + $"'{OptionalSourceSchemaFileListOption.OptionName}', or '{FusionArchiveFileOption.OptionName}'.");
            }
            else if (exclusiveOptionsCount < 1)
            {
                result.AddError(
                    $"You need to specify one of: '{OptionalSourceSchemaIdentifierListOption.OptionName}', "
                    + $"'{OptionalSourceSchemaFileListOption.OptionName}', or '{FusionArchiveFileOption.OptionName}'.");
            }
        });

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken) =>
        {
            var workingDirectory = parseResult.GetValue(Opt<WorkingDirectoryOption>.Instance)!;
            var sourceSchemaFiles =
                parseResult.GetValue(Opt<OptionalSourceSchemaFileListOption>.Instance) ?? [];
            var sourceSchemaIdentifiers =
                parseResult.GetValue(Opt<OptionalSourceSchemaIdentifierListOption>.Instance) ?? [];
            var archiveFile =
                parseResult.GetValue(Opt<OptionalFusionArchiveFileOption>.Instance);
            var stageName = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
            var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
            var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
            var sourceMetadataJson =
                parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

            return await ExecuteAsync(
                workingDirectory,
                sourceSchemaFiles,
                sourceSchemaIdentifiers,
                archiveFile,
                apiId,
                stageName,
                tag,
                sourceMetadataJson,
                console,
                fileSystem,
                fusionConfigurationClient,
                cancellationToken);
        });
    }

    private static async Task<int> ExecuteAsync(
        string workingDirectory,
        List<string> sourceSchemaFiles,
        List<string> sourceSchemaIdentifiers,
        string? archiveFile,
        string apiId,
        string stageName,
        string tag,
        string? sourceMetadataJson,
        INitroConsole console,
        IFileSystem fileSystem,
        IFusionConfigurationClient client,
        CancellationToken cancellationToken)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        if (archiveFile is not null)
        {
            if (!fileSystem.FileExists(archiveFile))
            {
                throw new ExitException($"Archive file '{archiveFile}' does not exist.");
            }

            return await PublishFusionConfigurationAsync(
                apiId,
                stageName,
                tag,
                archiveFile,
                source,
                console,
                fileSystem,
                client,
                cancellationToken);
        }

        var sourceSchemaVersions = sourceSchemaIdentifiers
            .Select(i => ParseSourceSchemaVersion(i, tag))
            .ToArray();

        if (sourceSchemaFiles.Count == 0 && sourceSchemaVersions.Length == 0)
        {
            sourceSchemaFiles.AddRange(
                fileSystem.GetFiles(workingDirectory, "*.graphql*", SearchOption.AllDirectories)
                    .Where(f => FusionComposeCommand.IsSchemaFile(Path.GetFileName(f))));
        }
        else
        {
            for (var i = 0; i < sourceSchemaFiles.Count; i++)
            {
                var sourceSchemaFile = sourceSchemaFiles[i];
                if (!Path.IsPathRooted(sourceSchemaFile))
                {
                    sourceSchemaFiles[i] = Path.Combine(workingDirectory, sourceSchemaFile);
                }
            }
        }

        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas;

        if (sourceSchemaFiles.Count > 0)
        {
            newSourceSchemas = await FusionComposeCommand.ReadSourceSchemasAsync(
                fileSystem,
                sourceSchemaFiles,
                cancellationToken);
        }
        else
        {
            newSourceSchemas = [];

            foreach (var sourceSchemaVersion in sourceSchemaVersions)
            {
                console.Log(
                    $"Downloading version '{sourceSchemaVersion.Version}' of source schema '{sourceSchemaVersion.Name}'...");

                await using var sourceSchemaArchiveStream =
                    await client.DownloadSourceSchemaArchiveAsync(
                    apiId,
                    sourceSchemaVersion.Name,
                    sourceSchemaVersion.Version,
                    cancellationToken);
                using var archive = FusionSourceSchemaArchive.Open(sourceSchemaArchiveStream);

                var settings = await archive.TryGetSettingsAsync(cancellationToken);

                if (settings is null)
                {
                    throw new ExitException(
                        $"Archive of source schema '{sourceSchemaVersion.Name}' does not contain source schema settings.");
                }

                var schema = await archive.TryGetSchemaAsync(cancellationToken);

                if (!schema.HasValue)
                {
                    throw new ExitException(
                        $"Archive of source schema '{sourceSchemaVersion.Name}' does not contain a GraphQL schema.");
                }

                var schemaName = sourceSchemaVersion.Name;
                var schemaText = Encoding.UTF8.GetString(schema.Value.Span);

                newSourceSchemas.Add(schemaName, (new SourceSchemaText(schemaName, schemaText), settings));
            }
        }

        return await PublishFusionConfigurationAsync(
            apiId,
            stageName,
            tag,
            newSourceSchemas,
            sourceSchemaVersions,
            compositionSettings: null,
            source,
            console,
            fileSystem,
            client,
            cancellationToken);
    }

    private static async Task<int> PublishFusionConfigurationAsync(
        string apiId,
        string stageName,
        string tag,
        string archiveFilePath,
        SourceMetadata? source,
        INitroConsole console,
        IFileSystem fileSystem,
        IFusionConfigurationClient client,
        CancellationToken cancellationToken)
    {
        await using var archiveStream = fileSystem.OpenReadStream(archiveFilePath);

        string requestId = null!;
        try
        {
            await using (var beginActivity = console.StartActivity("Requesting deployment slot..."))
            {
                requestId = await FusionPublishHelpers.RequestDeploymentSlotAsync(
                    apiId,
                    stageName,
                    tag,
                    subgraphId: null,
                    subgraphName: null,
                    sourceSchemaVersions: null,
                    waitForApproval: false,
                    source,
                    beginActivity,
                    console,
                    client,
                    cancellationToken);
            }

            await using (var _ = console.StartActivity("Claiming deployment slot..."))
            {
                await client.ClaimDeploymentSlotAsync(requestId, cancellationToken);
            }

            await using (var commitActivity = console.StartActivity(
                $"Uploading new configuration to '{stageName}'..."))
            {
                await FusionPublishHelpers.UploadFusionArchiveAsync(
                    requestId,
                    archiveStream,
                    commitActivity,
                    console,
                    client,
                    cancellationToken);
            }
        }
        catch (NitroClientException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await console.Error.WriteLineAsync(exception.Message);

            if (!string.IsNullOrEmpty(requestId))
            {
                await client.ReleaseDeploymentSlotAsync(requestId, CancellationToken.None);
            }

            return ExitCodes.Error;
        }

        return ExitCodes.Success;
    }

    private static async Task<int> PublishFusionConfigurationAsync(
        string apiId,
        string stageName,
        string tag,
        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas,
        SourceSchemaVersion[] sourceSchemaVersions,
        CompositionSettings? compositionSettings,
        SourceMetadata? source,
        INitroConsole console,
        IFileSystem fileSystem,
        IFusionConfigurationClient client,
        CancellationToken cancellationToken)
    {
        string requestId = null!;
        try
        {
            // begin
            await using (var beginActivity = console.StartActivity("Requesting deployment slot..."))
            {
                requestId = await RequestDeploymentSlotAsync(beginActivity);
            }

            // start
            await using (var _ = console.StartActivity("Claiming deployment slot..."))
            {
                await ClaimDeploymentSlotAsync();
            }

            // download
            Stream? existingArchiveStream;
            await using (var _ = console.StartActivity(
                $"Downloading existing configuration from '{stageName}'..."))
            {
                // TODO: Needs to handle old and new archive
                existingArchiveStream = await DownloadConfigurationAsync();
            }

            // compose
            bool success;
            await using Stream archiveStream = new MemoryStream();
            await using (var _ = console.StartActivity("Composing new configuration..."))
            {
                success = await FusionPublishHelpers.ComposeAsync(
                    archiveStream,
                    existingArchiveStream,
                    stageName,
                    newSourceSchemas,
                    compositionSettings,
                    console,
                    cancellationToken);

                if (success)
                {
                    console.Success("Composed new configuration.");
                }
                else
                {
                    console.ErrorLine("Failed to compose new configuration.");
                }
            }

            if (!success)
            {
                // cancel
                await client.ReleaseDeploymentSlotAsync(requestId, CancellationToken.None);

                return ExitCodes.Error;
            }

            // commit
            await using (var commitActivity = console.StartActivity(
                $"Uploading new configuration to '{stageName}'..."))
            {
                await UploadConfigurationAsync(archiveStream, commitActivity);
            }
        }
        catch (NitroClientException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await console.Error.WriteLineAsync(exception.Message);

            if (!string.IsNullOrEmpty(requestId))
            {
                await client.ReleaseDeploymentSlotAsync(requestId, CancellationToken.None);
            }

            return ExitCodes.Error;
        }

        return ExitCodes.Success;

        Task<string> RequestDeploymentSlotAsync(INitroConsoleActivity activity)
        {
            return FusionPublishHelpers.RequestDeploymentSlotAsync(
                apiId,
                stageName,
                tag,
                subgraphId: null,
                subgraphName: null,
                sourceSchemaVersions,
                waitForApproval: false,
                source,
                activity,
                console,
                client,
                cancellationToken);
        }

        async Task ClaimDeploymentSlotAsync()
        {
            await client.ClaimDeploymentSlotAsync(requestId, cancellationToken);

            console.Success("Claimed deployment slot.");
        }

        async Task<Stream?> DownloadConfigurationAsync()
        {
            Stream? stream;
            try
            {
                stream = await client.DownloadLatestFusionArchiveAsync(
                    apiId,
                    stageName,
                    cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.BadRequest)
            {
                stream = await client.DownloadLatestLegacyFusionArchiveAsync(
                    apiId,
                    stageName,
                    cancellationToken);
            }

            if (stream is null)
            {
                console.WarningLine($"There is no existing configuration on '{stageName}'.");
            }
            else
            {
                console.Success($"Downloaded an existing configuration from '{stageName}'.");
            }

            return stream;
        }

        async Task UploadConfigurationAsync(Stream stream, INitroConsoleActivity activity)
        {
            var success = await FusionPublishHelpers.UploadFusionArchiveAsync(
                requestId,
                stream,
                activity,
                console,
                client,
                cancellationToken);

            if (!success)
            {
                throw new ExitException("Configuration failed to upload.");
            }

            console.Success($"Uploaded new configuration '{tag}' to '{stageName}'.");
        }
    }

    private static SourceSchemaVersion ParseSourceSchemaVersion(string input, string tag)
    {
        var atIndex = input.LastIndexOf('@');

        if (atIndex > 0)
        {
            var name = input[..atIndex];
            var version = input[(atIndex + 1)..];

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("The source schema name before the '@' cannot be empty.", nameof(input));
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("The source schema version after the '@' cannot be empty.", nameof(input));
            }

            return new SourceSchemaVersion(name, version);
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("The source schema name cannot be empty.", nameof(input));
        }

        return new SourceSchemaVersion(input, tag);
    }
}
