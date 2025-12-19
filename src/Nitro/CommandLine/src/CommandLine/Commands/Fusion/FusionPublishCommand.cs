using System.CommandLine.IO;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Settings;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using static ChilliCream.Nitro.CommandLine.CommandLineResources;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal sealed class FusionPublishCommand : Command
{
    public FusionPublishCommand() : base("publish")
    {
        Description = "Publishes one or more source schemas as a new Fusion configuration to Nitro."
            + Environment.NewLine
            + "To take control over the deployment orchestration use sub-commands like 'begin'."
            + Environment.NewLine
            + "Since this command performs a Fusion composition internally, it only supports Fusion v2."
            + Environment.NewLine
            + "The orchestration sub-commands can also be used for Fusion v1.";

        AddCommand(new FusionConfigurationPublishBeginCommand());
        AddCommand(new FusionConfigurationPublishStartCommand());
        AddCommand(new FusionConfigurationPublishValidateCommand());
        AddCommand(new FusionConfigurationPublishCancelCommand());
        AddCommand(new FusionConfigurationPublishCommitCommand());

        var workingDirectoryOption = new Option<string>("--working-directory")
        {
            Description = ComposeCommand_WorkingDirectory_Description
        };
        workingDirectoryOption.AddAlias("-w");
        workingDirectoryOption.AddValidator(result =>
        {
            var workingDirectory = result.GetValueForOption(workingDirectoryOption);

            if (!Directory.Exists(workingDirectory))
            {
                result.ErrorMessage =
                    string.Format(
                        ComposeCommand_Error_WorkingDirectoryDoesNotExist,
                        workingDirectory);
            }
        });
        workingDirectoryOption.SetDefaultValueFactory(Directory.GetCurrentDirectory);
        workingDirectoryOption.LegalFilePathsOnly();

        var sourceSchemaFileOption = new Option<List<string>>("--source-schema-file")
        {
            Description = ComposeCommand_SourceSchemaFile_Description
        };
        sourceSchemaFileOption.AddAlias("-s");
        sourceSchemaFileOption.LegalFilePathsOnly();

        AddOption(workingDirectoryOption);
        AddOption(sourceSchemaFileOption);
        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);
        this.AddNitroCloudDefaultOptions();

        this.SetHandler(async context =>
        {
            var workingDirectory = context.ParseResult.GetValueForOption(workingDirectoryOption)!;
            var sourceSchemaFiles = context.ParseResult.GetValueForOption(sourceSchemaFileOption)!;
            var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
            var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
            var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;

            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            var apiClient = context.BindingContext.GetRequiredService<IApiClient>();
            var httpClientFactory = context.BindingContext.GetRequiredService<IHttpClientFactory>();

            context.ExitCode = await ExecuteAsync(
                workingDirectory,
                sourceSchemaFiles,
                apiId,
                stageName,
                tag,
                // We'll always take the settings already in the configuration for this
                compositionSettings: null,
                requireExistingConfiguration: false,
                console,
                apiClient,
                httpClientFactory,
                context.GetCancellationToken());
        });
    }

    public static async Task<int> ExecuteAsync(
        string? workingDirectory,
        List<string> sourceSchemaFiles,
        string apiId,
        string stageName,
        string tag,
        CompositionSettings? compositionSettings,
        bool requireExistingConfiguration,
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
                Stream? existingConfigurationStream = null;
                await console
                    .Status()
                    .Spinner(Spinner.Known.BouncingBar)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync(
                        $"Downloading existing configuration from '{stageName}'...",
                        async _ => existingConfigurationStream = await DownloadConfigurationAsync());

                // compose
                await using Stream archiveStream = new MemoryStream();

                var success = await ComposeAsync(archiveStream, existingConfigurationStream);

                if (!success)
                {
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
                var existingConfigurationStream = await DownloadConfigurationAsync();

                // compose
                await using Stream archiveStream = new MemoryStream();

                var success = await ComposeAsync(archiveStream, existingConfigurationStream);

                if (!success)
                {
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
            console.Error.WriteLine(exception.Message);

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
                null,
                null,
                false,
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
            var stream = await FusionPublishHelpers.DownloadConfigurationAsync(
                apiId,
                stageName,
                client,
                httpClientFactory,
                cancellationToken);

            if (stream is null)
            {
                if (requireExistingConfiguration)
                {
                    throw new ExitException($"Expected an existing configuration on '{stageName}'.");
                }

                console.WarningLine($"There is no existing configuration on '{stageName}'.");
            }
            else
            {
                console.Success($"Downloaded an existing configuration from '{stageName}'.");
            }

            return stream;
        }

        async Task<bool> ComposeAsync(Stream archiveStream, Stream? existingConfigurationStream)
        {
            FusionArchive archive;

            if (existingConfigurationStream is not null)
            {
                await existingConfigurationStream.CopyToAsync(archiveStream, cancellationToken);
                await existingConfigurationStream.DisposeAsync();

                archiveStream.Seek(0, SeekOrigin.Begin);

                archive = FusionArchive.Open(
                    archiveStream,
                    mode: FusionArchiveMode.Update,
                    leaveOpen: true);
            }
            else
            {
                archive = FusionArchive.Create(archiveStream, leaveOpen: true);
            }

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                if (sourceSchemaFiles.Count == 0)
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
            }

            var compositionLog = new CompositionLog();

            var result = await FusionComposeCommand.ComposeAsync(
                compositionLog,
                sourceSchemaFiles,
                archive,
                environment: stageName,
                compositionSettings,
                cancellationToken);

            var writer = new AnsiStreamWriter(result.IsSuccess ? console.Out : console.Error);

            FusionComposeCommand.WriteCompositionLog(
                compositionLog,
                writer,
                false);

            if (result.IsFailure)
            {
                foreach (var error in result.Errors)
                {
                    console.Error.WriteLine(error.Message);
                }

                return false;
            }

            archiveStream.Seek(0, SeekOrigin.Begin);

            return true;
        }

        async Task UploadConfigurationAsync(Stream stream, StatusContext? statusContext)
        {
            var success = await FusionPublishHelpers.UploadConfigurationAsync(
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

    private sealed class AnsiStreamWriter(TextWriter textWriter) : IStandardStreamWriter
    {
        public void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                textWriter.Write(value);
            }
        }
    }
}
