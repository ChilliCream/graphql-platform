#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
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
        Options.Add(Opt<OptionalForceOption>.Instance);
        Options.Add(Opt<OptionalWaitForApprovalOption>.Instance);
        Options.Add(Opt<WorkingDirectoryOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        Validators.Add(result =>
        {
            var forceResult = result.GetResult(Opt<OptionalForceOption>.Instance);
            var waitResult = result.GetResult(Opt<OptionalWaitForApprovalOption>.Instance);

            if (forceResult is { Implicit: false } && waitResult is { Implicit: false })
            {
                result.AddError(
                    "The '--force' and '--wait-for-approval' options are mutually exclusive.");
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
        var sessionService = services.GetRequiredService<ISessionService>();
        var fileSystem = services.GetRequiredService<IFileSystem>();

        parseResult.AssertHasAuthentication(sessionService);

        var workingDirectory = parseResult.GetValue(Opt<WorkingDirectoryOption>.Instance) ??
            fileSystem.GetCurrentDirectory();
        var sourceSchemaFiles =
            parseResult.GetValue(Opt<OptionalSourceSchemaFileListOption>.Instance) ?? [];
        var sourceSchemaIdentifiers =
            parseResult.GetValue(Opt<OptionalSourceSchemaIdentifierListOption>.Instance) ?? [];
        var archiveFile =
            parseResult.GetValue(Opt<OptionalFusionArchiveFileOption>.Instance);
        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var stageName = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var apiId = parseResult.GetRequiredValue(Opt<ApiIdOption>.Instance);
        var tag = parseResult.GetRequiredValue(Opt<TagOption>.Instance);
        var sourceMetadataJson =
            parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        var exclusiveOptionsCount = new[]
        {
            sourceSchemaFiles is { Count: > 0 },
            sourceSchemaIdentifiers is { Count: > 0 },
            archiveFile is not null
        }.Count(x => x);

        if (exclusiveOptionsCount > 1)
        {
            throw new ExitException(
                $"The options '{OptionalSourceSchemaIdentifierListOption.OptionName}', "
                + $"'{OptionalSourceSchemaFileListOption.OptionName}', and '{FusionArchiveFileOption.OptionName}' are mutually exclusive.");
        }
        else if (exclusiveOptionsCount < 1)
        {
            throw new ExitException(
                $"Missing one of the required options '{OptionalSourceSchemaIdentifierListOption.OptionName}', "
                + $"'{OptionalSourceSchemaFileListOption.OptionName}', or '{FusionArchiveFileOption.OptionName}'.");
        }

        if (archiveFile is not null)
        {
            return await PublishFusionConfigurationWithArchiveAsync();
        }
        else if (sourceSchemaFiles.Count > 0)
        {
            return await PublishFusionConfigurationWithSourceSchemaFilesAsync();
        }
        else
        {
            return await PublishFusionConfigurationWithSourceSchemasAsync();
        }

        async Task<int> PublishFusionConfigurationWithArchiveAsync()
        {
            if (!Path.IsPathRooted(archiveFile))
            {
                archiveFile = Path.Combine(workingDirectory, archiveFile);
            }

            if (!fileSystem.FileExists(archiveFile))
            {
                throw new ExitException(ErrorMessages.ArchiveFileDoesNotExist(archiveFile));
            }

            await using var activity = StartPublishActivity(console, stageName, apiId, force);
            await using var archiveStream = fileSystem.OpenReadStream(archiveFile);

            return await ExecutePublishAsync(
                activity,
                sourceSchemaVersions: null,
                prepareArchive: () => Task.FromResult(archiveStream));
        }

        async Task<int> PublishFusionConfigurationWithSourceSchemaFilesAsync()
        {
            for (var i = 0; i < sourceSchemaFiles.Count; i++)
            {
                var sourceSchemaFile = sourceSchemaFiles[i];

                if (!Path.IsPathRooted(sourceSchemaFile))
                {
                    sourceSchemaFiles[i] = sourceSchemaFile = Path.Combine(workingDirectory, sourceSchemaFile);
                }

                if (!fileSystem.FileExists(sourceSchemaFile))
                {
                    throw new ExitException(ErrorMessages.SchemaFileDoesNotExist(sourceSchemaFile));
                }
            }

            await using var activity = StartPublishActivity(console, stageName, apiId, force);

            var newSourceSchemas = await FusionComposeCommand.ReadSourceSchemasAsync(
                fileSystem,
                sourceSchemaFiles,
                cancellationToken);

            return await ExecutePublishAsync(
                activity,
                sourceSchemaVersions: null,
                prepareArchive: () => ComposeAsync(activity, newSourceSchemas));
        }

        async Task<int> PublishFusionConfigurationWithSourceSchemasAsync()
        {
            var sourceSchemaVersions = sourceSchemaIdentifiers
                .Select(i => ParseSourceSchemaVersion(i, tag))
                .ToArray();

            await using var activity = StartPublishActivity(console, stageName, apiId, force);

            var newSourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

            await using (var downloadSourceSchemaActivity = activity.StartChildActivity(
                             $"Downloading {sourceSchemaVersions.Length} source schema(s)",
                             "Failed to download source schemas."))
            {
                foreach (var sourceSchemaVersion in sourceSchemaVersions)
                {
                    await using var sourceSchemaArchiveStream =
                        await client.DownloadSourceSchemaArchiveAsync(
                            apiId,
                            sourceSchemaVersion.Name,
                            sourceSchemaVersion.Version,
                            cancellationToken);

                    if (sourceSchemaArchiveStream is null)
                    {
                        var errorMessage =
                            $"Could not find source schema '{sourceSchemaVersion.Name}' with version '{sourceSchemaVersion.Version}'.";
                        downloadSourceSchemaActivity.Fail(errorMessage);

                        throw new ExitException(errorMessage);
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

                downloadSourceSchemaActivity.Success($"Downloaded {sourceSchemaVersions.Length} source schema(s).");
            }

            return await ExecutePublishAsync(
                activity,
                sourceSchemaVersions,
                prepareArchive: () => ComposeAsync(activity, newSourceSchemas));
        }

        async Task<Stream> ComposeAsync(
            INitroConsoleActivity activity,
            Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas)
        {
            // download
            var existingArchiveStream = await DownloadExistingFusionConfigurationAsync(activity);

            // compose
            await using var composeActivity = activity.StartChildActivity(
                "Composing new configuration",
                "Failed to compose new configuration.");

            var archiveStream = new MemoryStream();
            var (result, compositionLog) = await FusionPublishHelpers.ComposeAsync(
                archiveStream,
                existingArchiveStream,
                stageName,
                newSourceSchemas,
                null,
                cancellationToken);

            if (result.IsSuccess)
            {
                composeActivity.Success("Composed new configuration.");
            }
            else
            {
                await composeActivity.FailAllAsync();

                console.WriteLine();
                console.WriteLine("## Composition log");
                console.WriteLine();

                FusionComposeCommand.WriteCompositionLog(
                    compositionLog,
                    console.Out,
                    false);

                foreach (var error in result.Errors)
                {
                    console.Error.WriteErrorLine(error.Message);
                }

                throw new ExitException();
            }

            return archiveStream;
        }

        async Task<int> ExecutePublishAsync(
            INitroConsoleActivity activity,
            SourceSchemaVersion[]? sourceSchemaVersions,
            Func<Task<Stream>> prepareArchive)
        {
            string? requestId = null;

            try
            {
                await using (var requestDeploymentSlotActivity = activity.StartChildActivity(
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
                        requestDeploymentSlotActivity,
                        console,
                        client,
                        cancellationToken);

                    requestDeploymentSlotActivity.Success("Deployment slot ready.");
                }

                await using (var claimDeploymentSlotActivity = activity.StartChildActivity(
                                 "Claiming deployment slot",
                                 "Failed to claim the deployment slot."))
                {
                    await FusionPublishHelpers.ClaimDeploymentSlotAsync(
                        requestId,
                        claimDeploymentSlotActivity,
                        console,
                        client,
                        cancellationToken);

                    claimDeploymentSlotActivity.Success("Claimed deployment slot.");
                }

                await using var archiveStream = await prepareArchive();

                // We only do validation if --wait-for-approval is not set.
                // If --wait-for-approval is set the validation will be done automatically during the publish step.
                if (!waitForApproval)
                {
                    // Since validation would consume the archive stream, we clone it here.
                    await using var clonedArchiveStream = new MemoryStream();
                    await archiveStream.CopyToAsync(clonedArchiveStream, cancellationToken);
                    clonedArchiveStream.Position = 0;
                    archiveStream.Position = 0;

                    await using var validationActivity = activity.StartChildActivity(
                        $"Validating configuration against '{stageName}'",
                        "Failed to validate the new configuration.");

                    var isValidArchive = await FusionPublishHelpers.ValidateFusionConfigurationAsync(
                        requestId,
                        clonedArchiveStream,
                        validationActivity,
                        console,
                        client,
                        cancellationToken);

                    if (isValidArchive)
                    {
                        validationActivity.Success("Validated configuration.");
                    }
                    else if (!force)
                    {
                        throw new ExitException("Failed to validate configuration.");
                    }
                }

                bool uploaded;

                await using (var uploadActivity = activity.StartChildActivity(
                                 $"Uploading configuration to '{stageName}'",
                                 "Failed to upload the new configuration."))
                {
                    uploaded = await FusionPublishHelpers.UploadFusionConfigurationAsync(
                        requestId,
                        archiveStream,
                        uploadActivity,
                        console,
                        client,
                        cancellationToken);

                    if (uploaded)
                    {
                        uploadActivity.Success("Uploaded configuration.");
                    }
                }

                if (uploaded)
                {
                    activity.Success($"Published configuration '{tag}' to '{stageName}'.");

                    return ExitCodes.Success;
                }

                return ExitCodes.Error;
            }
            catch
            {
                if (!string.IsNullOrEmpty(requestId))
                {
                    try
                    {
                        var releaseResult = await client.ReleaseDeploymentSlotAsync(requestId, CancellationToken.None);

                        if (releaseResult.Errors is { Count: > 0 })
                        {
                            console.Error.WriteErrorLine(
                                "Encountered the following errors while trying to release the deployment slot after an error during the publishing process:");

                            foreach (var error in releaseResult.Errors)
                            {
                                var errorMessage = error switch
                                {
                                    IUnauthorizedOperation err => err.Message,
                                    IFusionConfigurationRequestNotFoundError err => err.Message,
                                    IInvalidProcessingStateTransitionError err => err.Message,
                                    IError err => ErrorMessages.UnexpectedMutationError(err),
                                    _ => ErrorMessages.UnexpectedMutationError()
                                };

                                console.Error.WriteErrorLine(errorMessage);
                            }

                            console.Error.WriteErrorLine("This is the error that caused the publishing process to fail in the first place:");
                        }
                    }
                    catch (Exception exception)
                    {
                        console.Error.WriteErrorLine(
                            "Encountered an unexpected exception while trying to release the deployment slot after an error during the publishing process:");
                        console.Error.WriteErrorLine(exception.Message);
                        console.Error.WriteErrorLine("This is the error that caused the publishing process to fail in the first place:");
                    }
                }

                throw;
            }
        }

        async Task<Stream?> DownloadExistingFusionConfigurationAsync(INitroConsoleActivity activity)
        {
            await using var downloadActivity = activity.StartChildActivity(
                $"Downloading existing configuration from '{stageName}'",
                "Failed to download the existing Fusion configuration.");

            var existingArchiveStream = await client.DownloadLatestFusionArchiveAsync(
                apiId,
                stageName,
                WellKnownVersions.LatestGatewayFormatVersion.ToString(),
                ArchiveFormats.Far,
                cancellationToken);

            if (existingArchiveStream is null)
            {
                downloadActivity.Warning($"There is no existing configuration on '{stageName}'.");
            }
            else
            {
                downloadActivity.Success($"Downloaded existing configuration from '{stageName}'.");
            }

            return existingArchiveStream;
        }
    }

    private static INitroConsoleActivity StartPublishActivity(
        INitroConsole console,
        string stageName,
        string apiId,
        bool force)
    {
        var activity = console.StartActivity(
            $"Publishing Fusion configuration to stage '{stageName}' of API '{apiId.EscapeMarkup()}'",
            "Failed to publish Fusion configuration.");

        if (force)
        {
            activity.Update("Force push is enabled.", ActivityUpdateKind.Warning);
        }

        return activity;
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
