using System.CommandLine.IO;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using HotChocolate.Fusion.CommandLine;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Fusion;

internal sealed class FusionPublishCommand : Command
{
    public FusionPublishCommand() : base("publish")
    {
        Description = "TODO";

        var workingDirectoryOption = new Option<string>("--working-directory")
        {
            // Description = ComposeCommand_WorkingDirectory_Description
        };
        workingDirectoryOption.AddAlias("-w");
        workingDirectoryOption.AddValidator(result =>
        {
            var workingDirectory = result.GetValueForOption(workingDirectoryOption);

            if (!Directory.Exists(workingDirectory))
            {
                result.ErrorMessage = "Working directory doesn't exist";
                // result.ErrorMessage =
                //     string.Format(
                //         ComposeCommand_Error_WorkingDirectoryDoesNotExist,
                //         workingDirectory);
            }
        });
        workingDirectoryOption.SetDefaultValueFactory(Directory.GetCurrentDirectory);
        workingDirectoryOption.LegalFilePathsOnly();

        var sourceSchemaFileOption = new Option<List<string>>("--source-schema-file")
        {
            // Description = ComposeCommand_SourceSchemaFile_Description
        };
        sourceSchemaFileOption.AddAlias("-s");
        sourceSchemaFileOption.LegalFilePathsOnly();

        AddOption(workingDirectoryOption);
        AddOption(sourceSchemaFileOption);
        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);

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
                console,
                apiClient,
                httpClientFactory,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        string workingDirectory,
        List<string> sourceSchemaFiles,
        string apiId,
        string stageName,
        string tag,
        IAnsiConsole console,
        IApiClient client,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        string requestId = null!;
        try
        {
            if (console.IsHumandReadable())
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
                        async _ => await ClaimDeploymentSlotAsync(requestId));

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
                    await FusionConfigurationPublishHelpers.ReleaseDeploymentSlot(
                        requestId,
                        console,
                        client,
                        CancellationToken.None);

                    return CommandLine.ExitCodes.Error;
                }

                // commit
                await console
                    .Status()
                    .Spinner(Spinner.Known.BouncingBar)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync(
                        $"Uploading new configuration to '{stageName}'...",
                        async context => await UploadConfigurationAsync(requestId, archiveStream, context));
            }
            else
            {
                // begin
                console.WriteLine("Requesting deployment slot...");
                requestId = await RequestDeploymentSlotAsync(null);

                // start
                console.WriteLine("Claiming deployment slot...");
                await ClaimDeploymentSlotAsync(requestId);

                // download
                console.WriteLine($"Downloading existing configuration from '{stageName}'...");
                var existingConfigurationStream = await DownloadConfigurationAsync();

                // compose
                await using Stream archiveStream = new MemoryStream();

                var success = await ComposeAsync(archiveStream, existingConfigurationStream);

                if (!success)
                {
                    await FusionConfigurationPublishHelpers.ReleaseDeploymentSlot(
                        requestId,
                        console,
                        client,
                        CancellationToken.None);

                    return CommandLine.ExitCodes.Error;
                }

                // commit
                console.WriteLine($"Uploading new configuration to '{stageName}'...");
                await UploadConfigurationAsync(requestId, archiveStream, null);
            }
        }
        catch (Exception exception)
        {
            // TODO: Use stderr
            console.Error(exception.Message);

            if (!string.IsNullOrEmpty(requestId))
            {
                await FusionConfigurationPublishHelpers.ReleaseDeploymentSlot(
                    requestId,
                    console,
                    client,
                    CancellationToken.None);
            }
        }

        return ExitCodes.Success;

        Task<string> RequestDeploymentSlotAsync(StatusContext? statusContext)
        {
            return FusionConfigurationPublishHelpers.RequestDeploymentSlotAsync(
                apiId,
                stageName,
                tag,
                // As we could be publishing multiple subgraphs, we do not associate this publish with a specific one.
                null,
                "TEMP", // TODO: Get rid of this as soon as the Backend is updated
                false,
                statusContext,
                console,
                client,
                cancellationToken);
        }

        async Task ClaimDeploymentSlotAsync(string requestId)
        {
            await FusionConfigurationPublishHelpers.ClaimDeploymentSlot(
                requestId,
                console,
                client,
                cancellationToken);

            console.Success("Claimed deployment slot.");
        }

        async Task<Stream?> DownloadConfigurationAsync()
        {
            var stream = await FusionConfigurationPublishHelpers.DownloadConfigurationAsync(
                apiId,
                stageName,
                client,
                httpClientFactory,
                cancellationToken);

            if (stream is null)
            {
                console.WarningLine($"There is not existing configuration on '{stageName}'.");
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

            if (sourceSchemaFiles.Count == 0)
            {
                sourceSchemaFiles.AddRange(
                    new DirectoryInfo(workingDirectory)
                        .GetFiles("*.graphql*", SearchOption.AllDirectories)
                        .Where(f => ComposeCommand.IsSchemaFile(f.Name))
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

            var compositionLog = new CompositionLog();

            var result = await ComposeCommand.ComposeAsync(
                compositionLog,
                sourceSchemaFiles,
                archive,
                null,
                true, // TODO: This should come from the existing gateway file
                cancellationToken);

            ComposeCommand.WriteCompositionLog(
                compositionLog,
                new AnsiStreamWriter(console),
                false);

            if (result.IsFailure)
            {
                foreach (var error in result.Errors)
                {
                    // TODO: Use stderr
                    console.Error(error.Message);
                }

                return false;
            }

            archiveStream.Seek(0, SeekOrigin.Begin);

            return true;
        }

        async Task UploadConfigurationAsync(string requestId, Stream stream, StatusContext? statusContext)
        {
            var success = await FusionConfigurationPublishHelpers.UploadConfigurationAsync(
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

    private sealed class AnsiStreamWriter(IAnsiConsole console) : IStandardStreamWriter
    {
        public void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                console.Write(value);
            }
        }
    }
}
