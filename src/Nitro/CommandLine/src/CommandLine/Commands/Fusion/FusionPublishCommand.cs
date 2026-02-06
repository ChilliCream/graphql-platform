#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionPublishCommand : Command
{
    public FusionPublishCommand() : base("publish")
    {
        Description = "Publishes a Fusion archive to Nitro."
            + Environment.NewLine
            + "To take control over the deployment orchestration use sub-commands like 'begin'."
            + Environment.NewLine
            + "If you don't specify --archive and instead use --source-schema or --source-schema-file, a Fusion v2 composition will be performed internally."
            + Environment.NewLine
            + "The orchestration sub-commands can be used for both Fusion v1 and v2.";

        AddCommand(new FusionConfigurationPublishBeginCommand());
        AddCommand(new FusionConfigurationPublishStartCommand());
        AddCommand(new FusionConfigurationPublishValidateCommand());
        AddCommand(new FusionConfigurationPublishCancelCommand());
        AddCommand(new FusionConfigurationPublishCommitCommand());

        var archiveOption = new FusionArchiveFileOption(isRequired: false);

        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<SourceSchemaIdentifierListOption>.Instance);
        AddOption(Opt<SourceSchemaFileListOption>.Instance);
        AddOption(archiveOption);
        AddOption(Opt<WorkingDirectoryOption>.Instance);
        this.AddNitroCloudDefaultOptions();

        AddValidator(result =>
        {
            var exclusiveOptionsCount = new[]
            {
                result.FindResultFor(Opt<SourceSchemaFileListOption>.Instance) is not null,
                result.FindResultFor(Opt<SourceSchemaIdentifierListOption>.Instance) is not null,
                result.FindResultFor(archiveOption) is not null
            }.Count(x => x);

            if (exclusiveOptionsCount > 1)
            {
                result.ErrorMessage = "You can only specify one of: '--source-schema', '--source-schema-file', or '--archive'.";
            }
            else if (exclusiveOptionsCount < 1)
            {
                result.ErrorMessage = "You need to specify one of: '--source-schema', '--source-schema-file', or '--archive'.";
            }
        });

        this.SetHandler(async context =>
        {
            var workingDirectory = context.ParseResult.GetValueForOption(Opt<WorkingDirectoryOption>.Instance)!;
            var sourceSchemaFiles = context.ParseResult.GetValueForOption(Opt<SourceSchemaFileListOption>.Instance) ?? [];
            var sourceSchemaIdentifiers = context.ParseResult.GetValueForOption(Opt<SourceSchemaIdentifierListOption>.Instance) ?? [];
            var archiveFile = context.ParseResult.GetValueForOption(archiveOption);
            var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
            var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
            var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;

            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            var apiClient = context.BindingContext.GetRequiredService<IApiClient>();
            var httpClientFactory = context.BindingContext.GetRequiredService<IHttpClientFactory>();

            context.ExitCode = await ExecuteAsync(
                workingDirectory,
                sourceSchemaFiles,
                sourceSchemaIdentifiers,
                archiveFile,
                apiId,
                stageName,
                tag,
                console,
                apiClient,
                httpClientFactory,
                context.GetCancellationToken());
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
        IAnsiConsole console,
        IApiClient client,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        if (archiveFile is not null)
        {
            if (!File.Exists(archiveFile))
            {
                throw new ExitException($"Archive file '{archiveFile}' does not exist.");
            }

            return await PublishFusionConfigurationAsync(
                apiId,
                stageName,
                tag,
                archiveFile,
                console,
                client,
                httpClientFactory,
                cancellationToken);
        }

        var sourceSchemaVersions = sourceSchemaIdentifiers
            .Select(i => ParseSourceSchemaVersion(i, tag))
            .ToArray();

        if (sourceSchemaFiles.Count == 0 && sourceSchemaVersions.Length == 0)
        {
            sourceSchemaFiles.AddRange(
                new DirectoryInfo(workingDirectory)
                    .GetFiles("*.graphql*", SearchOption.AllDirectories)
                    .Where(f => FusionComposeCommand.IsSchemaFile(f.Name))
                    .Select(i => i.FullName));
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
                sourceSchemaFiles,
                cancellationToken);
        }
        else
        {
            newSourceSchemas = [];

            foreach (var sourceSchemaVersion in sourceSchemaVersions)
            {
                console.Log($"Downloading version '{sourceSchemaVersion.Version}' of source schema '{sourceSchemaVersion.Name}'...");

                using var archive = await FusionPublishHelpers.DownloadSourceSchemaArchiveAsync(
                    apiId,
                    sourceSchemaVersion.Name,
                    sourceSchemaVersion.Version,
                    httpClientFactory,
                    cancellationToken);

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
            compositionSettings: null,
            console,
            client,
            httpClientFactory,
            cancellationToken);
    }

    internal static async Task<int> PublishFusionConfigurationAsync(
        string apiId,
        string stageName,
        string tag,
        string archiveFilePath,
        IAnsiConsole console,
        IApiClient client,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        await using var archiveStream = File.Open(archiveFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        string requestId = null!;
        try
        {
            if (console.IsHumanReadable())
            {
                // begin
                await console
                    .Status()
                    .Spinner(Spinner.Known.BouncingBar)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync(
                        "Requesting deployment slot...",
                        async context =>
                        {
                            requestId = await FusionPublishHelpers.RequestDeploymentSlotAsync(
                                apiId,
                                stageName,
                                tag,
                                subgraphId: null,
                                subgraphName: null,
                                waitForApproval: false,
                                context,
                                console,
                                client,
                                cancellationToken);;
                        });

                // start
                await console
                    .Status()
                    .Spinner(Spinner.Known.BouncingBar)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync(
                        "Claiming deployment slot...",
                        async _ =>
                        {
                            await FusionPublishHelpers.ClaimDeploymentSlot(
                                requestId,
                                console,
                                client,
                                cancellationToken);
                        });

                // commit
                await console
                    .Status()
                    .Spinner(Spinner.Known.BouncingBar)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync(
                        $"Uploading new configuration to '{stageName}'...",
                        async context =>
                        {
                            await FusionPublishHelpers.UploadFusionArchiveAsync(
                                requestId,
                                archiveStream,
                                context,
                                console,
                                client,
                                cancellationToken);
                        });
            }
            else
            {
                // begin
                console.WriteLine("Requesting deployment slot...");
                requestId = await FusionPublishHelpers.RequestDeploymentSlotAsync(
                    apiId,
                    stageName,
                    tag,
                    subgraphId: null,
                    subgraphName: null,
                    waitForApproval: false,
                    statusContext: null,
                    console,
                    client,
                    cancellationToken);

                // start
                console.WriteLine("Claiming deployment slot...");
                await FusionPublishHelpers.ClaimDeploymentSlot(
                    requestId,
                    console,
                    client,
                    cancellationToken);

                // commit
                console.WriteLine($"Uploading new configuration to '{stageName}'...");
                await FusionPublishHelpers.UploadFusionArchiveAsync(
                    requestId,
                    archiveStream,
                    statusContext: null,
                    console,
                    client,
                    cancellationToken);
            }
        }
        catch (Exception exception)
        {
            console.WriteLine(exception.Message);

            if (!string.IsNullOrEmpty(requestId))
            {
                await FusionPublishHelpers.ReleaseDeploymentSlot(
                    requestId,
                    console,
                    client,
                    CancellationToken.None);
            }
        }

        return ExitCodes.Success;
    }

    internal static async Task<int> PublishFusionConfigurationAsync(
        string apiId,
        string stageName,
        string tag,
        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas,
        CompositionSettings? compositionSettings,
        IAnsiConsole console,
        IApiClient client,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        string requestId = null!;
        try
        {
            if (console.IsHumanReadable())
            {
                // begin
                await console
                    .Status()
                    .Spinner(Spinner.Known.BouncingBar)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync(
                        "Requesting deployment slot...",
                        async context => requestId = await RequestDeploymentSlotAsync(context));

                // start
                await console
                    .Status()
                    .Spinner(Spinner.Known.BouncingBar)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync(
                        "Claiming deployment slot...",
                        async _ => await ClaimDeploymentSlotAsync());

                // download
                Stream? exitingArchiveStream = null;
                await console
                    .Status()
                    .Spinner(Spinner.Known.BouncingBar)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync(
                        $"Downloading existing configuration from '{stageName}'...",
                        async _ => exitingArchiveStream = await DownloadConfigurationAsync());

                // compose
                await using Stream archiveStream = new MemoryStream();

                var success = await FusionPublishHelpers.ComposeAsync(
                    archiveStream,
                    exitingArchiveStream,
                    stageName,
                    newSourceSchemas,
                    compositionSettings,
                    console,
                    cancellationToken);

                if (!success)
                {
                    // cancel
                    await FusionPublishHelpers.ReleaseDeploymentSlot(
                        requestId,
                        console,
                        client,
                        CancellationToken.None);

                    return ExitCodes.Error;
                }

                // commit
                await console
                    .Status()
                    .Spinner(Spinner.Known.BouncingBar)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync(
                        $"Uploading new configuration to '{stageName}'...",
                        async context => await UploadConfigurationAsync(archiveStream, context));
            }
            else
            {
                // begin
                console.WriteLine("Requesting deployment slot...");
                requestId = await RequestDeploymentSlotAsync(null);

                // start
                console.WriteLine("Claiming deployment slot...");
                await ClaimDeploymentSlotAsync();

                // download
                console.WriteLine($"Downloading existing configuration from '{stageName}'...");
                var exitingArchiveStream = await DownloadConfigurationAsync();

                // compose
                await using Stream archiveStream = new MemoryStream();

                var success = await FusionPublishHelpers.ComposeAsync(
                    archiveStream,
                    exitingArchiveStream,
                    stageName,
                    newSourceSchemas,
                    compositionSettings,
                    console,
                    cancellationToken);

                if (!success)
                {
                    // cancel
                    await FusionPublishHelpers.ReleaseDeploymentSlot(
                        requestId,
                        console,
                        client,
                        CancellationToken.None);

                    return ExitCodes.Error;
                }

                // commit
                console.WriteLine($"Uploading new configuration to '{stageName}'...");
                await UploadConfigurationAsync(archiveStream, null);
            }
        }
        catch (Exception exception)
        {
            console.WriteLine(exception.Message);

            if (!string.IsNullOrEmpty(requestId))
            {
                await FusionPublishHelpers.ReleaseDeploymentSlot(
                    requestId,
                    console,
                    client,
                    CancellationToken.None);
            }
        }

        return ExitCodes.Success;

        Task<string> RequestDeploymentSlotAsync(StatusContext? statusContext)
        {
            return FusionPublishHelpers.RequestDeploymentSlotAsync(
                apiId,
                stageName,
                tag,
                // As we could be publishing multiple source schemas,
                // we do not associate this publish with a specific subgraph.
                subgraphId: null,
                subgraphName: null,
                waitForApproval: false,
                statusContext,
                console,
                client,
                cancellationToken);
        }

        async Task ClaimDeploymentSlotAsync()
        {
            await FusionPublishHelpers.ClaimDeploymentSlot(
                requestId,
                console,
                client,
                cancellationToken);

            console.Success("Claimed deployment slot.");
        }

        async Task<Stream?> DownloadConfigurationAsync()
        {
            var stream = await FusionPublishHelpers.DownloadLatestFusionArchiveAsync(
                apiId,
                stageName,
                client,
                httpClientFactory,
                cancellationToken);

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

        async Task UploadConfigurationAsync(Stream stream, StatusContext? statusContext)
        {
            var success = await FusionPublishHelpers.UploadFusionArchiveAsync(
                requestId,
                stream,
                statusContext,
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

    private sealed record SourceSchemaVersion(string Name, string Version);
}
