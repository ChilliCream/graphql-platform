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
using ChilliCream.Nitro.Client.FusionConfiguration;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
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
    public FusionPublishCommand() : base("publish")
    {
        Description = "Publish a Fusion configuration to a stage.";

        Subcommands.Add(new FusionConfigurationPublishBeginCommand());
        Subcommands.Add(new FusionConfigurationPublishStartCommand());
        Subcommands.Add(new FusionConfigurationPublishValidateCommand());
        Subcommands.Add(new FusionConfigurationPublishCancelCommand());
        Subcommands.Add(new FusionConfigurationPublishCommitCommand());

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<OptionalSourceSchemaIdentifierListOption>.Instance);
        Options.Add(Opt<OptionalSourceSchemaFileListOption>.Instance);
        Options.Add(Opt<OptionalFusionArchiveFileOption>.Instance);
        Options.Add(Opt<OptionalWaitForApprovalOption>.Instance);
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
                    $"The options '{OptionalSourceSchemaIdentifierListOption.OptionName}', "
                    + $"'{OptionalSourceSchemaFileListOption.OptionName}', and '{FusionArchiveFileOption.OptionName}' are mutually exclusive.");
            }
            else if (exclusiveOptionsCount < 1)
            {
                result.AddError(
                    $"Missing one of the required options '{OptionalSourceSchemaIdentifierListOption.OptionName}', "
                    + $"'{OptionalSourceSchemaFileListOption.OptionName}', or '{FusionArchiveFileOption.OptionName}'.");
            }
        });

        this.AddExamples(
            """
            fusion publish \
              --api-id "<api-id>" \
              --stage "dev" \
              --tag "v1" \
              --source-schema products \
              --source-schema reviews
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IFusionConfigurationClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();

        var workingDirectory = parseResult.GetValue(Opt<WorkingDirectoryOption>.Instance)
            ?? fileSystem.GetCurrentDirectory();
        var sourceSchemaFiles =
            parseResult.GetValue(Opt<OptionalSourceSchemaFileListOption>.Instance) ?? [];
        var sourceSchemaIdentifiers =
            parseResult.GetValue(Opt<OptionalSourceSchemaIdentifierListOption>.Instance) ?? [];
        var archiveFile =
            parseResult.GetValue(Opt<OptionalFusionArchiveFileOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var stageName = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var sourceMetadataJson =
            parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);
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
                waitForApproval,
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

            await using (var downloadActivity = console.StartActivity(
                "Downloading source schemas...",
                "Failed to download source schemas."))
            {
                foreach (var sourceSchemaVersion in sourceSchemaVersions)
                {
                    downloadActivity.Update(
                        $"Downloading '{sourceSchemaVersion.Name}' version '{sourceSchemaVersion.Version}'...");

                    await using var sourceSchemaArchiveStream =
                        await client.DownloadSourceSchemaArchiveAsync(
                        apiId,
                        sourceSchemaVersion.Name,
                        sourceSchemaVersion.Version,
                        cancellationToken);

                    if (sourceSchemaArchiveStream is null)
                    {
                        throw new ExitException(
                            $"Failed to download archive for source schema '{sourceSchemaVersion.Name}' version '{sourceSchemaVersion.Version}'.");
                    }

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

                downloadActivity.Success($"Downloaded {sourceSchemaVersions.Length} source schema(s).");
            }
        }

        return await PublishFusionConfigurationAsync(
            apiId,
            stageName,
            tag,
            newSourceSchemas,
            sourceSchemaVersions,
            compositionSettings: null,
            waitForApproval,
            source,
            console,
            client,
            cancellationToken);
    }

    private static async Task<int> PublishFusionConfigurationAsync(
        string apiId,
        string stageName,
        string tag,
        string archiveFilePath,
        bool waitForApproval,
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
            await using (var root = console.StartActivity(
                $"Publishing Fusion configuration to stage '{stageName}' of API '{apiId.EscapeMarkup()}'",
                "Failed to publish Fusion configuration."))
            {
                await using (var beginChild = root.StartChildActivity(
                    "Requesting deployment slot",
                    "Failed to request a deployment slot."))
                {
                    requestId = await FusionPublishHelpers.RequestDeploymentSlotAsync(
                        apiId,
                        stageName,
                        tag,
                        subgraphId: null,
                        subgraphName: null,
                        sourceSchemaVersions: null,
                        waitForApproval,
                        source,
                        beginChild,
                        console,
                        client,
                        cancellationToken);

                    beginChild.Success("Deployment slot ready.");
                }

                await using (var claimChild = root.StartChildActivity(
                    "Claiming deployment slot",
                    "Failed to claim the deployment slot."))
                {
                    await client.ClaimDeploymentSlotAsync(requestId, cancellationToken);

                    claimChild.Success("Claimed deployment slot.");
                }

                await using (var uploadChild = root.StartChildActivity(
                    $"Uploading configuration to '{stageName}'",
                    "Failed to upload the new configuration."))
                {
                    await FusionPublishHelpers.UploadFusionArchiveAsync(
                        requestId,
                        archiveStream,
                        uploadChild,
                        console,
                        client,
                        cancellationToken);

                    uploadChild.Success("Uploaded configuration.");
                }

                root.Success($"Published configuration to '{stageName}'.");
            }
        }
        catch (NitroClientException)
        {
            throw;
        }
        catch (Exception exception)
        {
            console.Error.WriteErrorLine(exception.Message);

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
        bool waitForApproval,
        SourceMetadata? source,
        INitroConsole console,
        IFusionConfigurationClient client,
        CancellationToken cancellationToken)
    {
        CompositionLog? failedLog = null;
        string requestId = null!;

        try
        {
            await using (var root = console.StartActivity(
                $"Publishing Fusion configuration to stage '{stageName}' of API '{apiId.EscapeMarkup()}'",
                "Failed to publish Fusion configuration."))
            {
                // begin
                await using (var beginChild = root.StartChildActivity(
                    "Requesting deployment slot",
                    "Failed to request a deployment slot."))
                {
                    requestId = await FusionPublishHelpers.RequestDeploymentSlotAsync(
                        apiId,
                        stageName,
                        tag,
                        subgraphId: null,
                        subgraphName: null,
                        sourceSchemaVersions,
                        waitForApproval,
                        source,
                        beginChild,
                        console,
                        client,
                        cancellationToken);

                    beginChild.Success("Deployment slot ready.");
                }

                // start
                await using (var claimChild = root.StartChildActivity(
                    "Claiming deployment slot",
                    "Failed to claim the deployment slot."))
                {
                    await client.ClaimDeploymentSlotAsync(requestId, cancellationToken);

                    claimChild.Success("Claimed deployment slot.");
                }

                // download
                Stream? existingArchiveStream;
                await using (var downloadChild = root.StartChildActivity(
                    $"Downloading existing configuration from '{stageName}'",
                    "Failed to download the existing Fusion configuration."))
                {
                    existingArchiveStream = await client.DownloadLatestFusionArchiveAsync(
                        apiId,
                        stageName,
                        WellKnownVersions.LatestGatewayFormatVersion.ToString(),
                        cancellationToken);

                    if (existingArchiveStream is null)
                    {
                        downloadChild.Update($"There is no existing configuration on '{stageName}'.", ActivityUpdateKind.Warning);
                    }
                    else
                    {
                        downloadChild.Success($"Downloaded existing configuration from '{stageName}'.");
                    }
                }

                // compose
                bool success;
                await using Stream archiveStream = new MemoryStream();
                await using (var composeChild = root.StartChildActivity(
                    "Composing new configuration",
                    "Failed to compose new configuration."))
                {
                    var composeResult = await FusionPublishHelpers.ComposeAsync(
                        archiveStream,
                        existingArchiveStream,
                        stageName,
                        newSourceSchemas,
                        compositionSettings,
                        cancellationToken);

                    success = composeResult.Success;

                    if (success)
                    {
                        composeChild.Success("Composed new configuration.");
                    }
                    else
                    {
                        if (!composeResult.Log.IsEmpty)
                        {
                            failedLog = composeResult.Log;
                        }

                        console.Error.WriteErrorLine("Failed to compose new configuration.");
                    }
                }

                if (!success)
                {
                    await client.ReleaseDeploymentSlotAsync(requestId, CancellationToken.None);

                    return ExitCodes.Error;
                }

                // commit
                await using (var uploadChild = root.StartChildActivity(
                    $"Uploading configuration to '{stageName}'",
                    "Failed to upload the new configuration."))
                {
                    var uploaded = await FusionPublishHelpers.UploadFusionArchiveAsync(
                        requestId,
                        archiveStream,
                        uploadChild,
                        console,
                        client,
                        cancellationToken);

                    if (!uploaded)
                    {
                        throw new ExitException("Configuration failed to upload.");
                    }

                    uploadChild.Success($"Uploaded new configuration '{tag}' to '{stageName}'.");
                }

                root.Success($"Published configuration '{tag}' to '{stageName}'.");
            }

            if (failedLog is { IsEmpty: false } log)
            {
                FusionComposeCommand.WriteCompositionLog(log, console.Out, false);
            }
        }
        catch (NitroClientException)
        {
            throw;
        }
        catch (Exception exception)
        {
            console.Error.WriteErrorLine(exception.Message);

            if (!string.IsNullOrEmpty(requestId))
            {
                await client.ReleaseDeploymentSlotAsync(requestId, CancellationToken.None);
            }

            return ExitCodes.Error;
        }

        return ExitCodes.Success;
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
