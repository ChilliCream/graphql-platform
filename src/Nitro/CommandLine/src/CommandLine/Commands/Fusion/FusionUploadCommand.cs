using System.Text;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using HotChocolate.Fusion.SourceSchema.Packaging;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.CommandLineResources;
using ArchiveMetadata = HotChocolate.Fusion.SourceSchema.Packaging.ArchiveMetadata;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

public sealed class FusionUploadCommand : Command
{
    public FusionUploadCommand() : base("upload")
    {
        Description = "Upload a source schema for a later composition.";

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

        var sourceSchemaFileOption = new Option<FileInfo>("--source-schema-file")
        {
            Description = ComposeCommand_SourceSchemaFile_Description
        };
        sourceSchemaFileOption.AddAlias("-s");
        sourceSchemaFileOption.LegalFilePathsOnly();

        this.AddNitroCloudDefaultOptions();

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(workingDirectoryOption);
        AddOption(sourceSchemaFileOption);

        this.SetHandler(async context =>
        {
            var workingDirectory = context.ParseResult.GetValueForOption(workingDirectoryOption)!;
            var sourceSchemaFile = context.ParseResult.GetValueForOption(sourceSchemaFileOption)!;
            var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
            var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;

            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            var apiClient = context.BindingContext.GetRequiredService<IApiClient>();

            context.ExitCode = await ExecuteAsync(
                console,
                apiClient,
                workingDirectory,
                sourceSchemaFile,
                tag,
                apiId,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string workingDirectory,
        FileInfo sourceSchemaFile,
        string tag,
        string apiId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(workingDirectory))
        {
            throw new ExitException("Expected a non-empty value for '--working-directory'.");
        }

        if (string.IsNullOrEmpty(apiId))
        {
            throw new ExitException("Expected a non-empty value for '--api-id'.");
        }

        if (string.IsNullOrEmpty(tag))
        {
            throw new ExitException("Expected a non-empty value for '--tag'.");
        }

        console.Title("Upload source schema");

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Uploading source schema...", UploadSourceSchemaFile);
        }
        else
        {
            await UploadSourceSchemaFile(null);
        }

        return ExitCodes.Success;

        async Task UploadSourceSchemaFile(StatusContext? ctx)
        {
            var sourceSchemaFilePath = sourceSchemaFile.FullName;

            if (!Path.IsPathRooted(sourceSchemaFilePath))
            {
                sourceSchemaFilePath = Path.Combine(workingDirectory, sourceSchemaFilePath);
            }

            var (_, sourceText, settings) = await FusionComposeCommand.ReadSourceSchemaAsync(
                sourceSchemaFilePath,
                cancellationToken);

            console.Log($"Uploading source schema at '{sourceSchemaFilePath}'...");

            await using var archiveStream = new MemoryStream();
            var archive = FusionSourceSchemaArchive.Create(archiveStream, leaveOpen: true);

            await archive.SetArchiveMetadataAsync(new ArchiveMetadata(), cancellationToken);
            await archive.SetSchemaAsync(
                Encoding.UTF8.GetBytes(sourceText.SourceText),
                cancellationToken);
            await archive.SetSettingsAsync(settings, cancellationToken);

            await archive.CommitAsync(cancellationToken);
            archive.Dispose();

            archiveStream.Position = 0;

            var input = new UploadFusionSubgraphInput
            {
                Archive = new Upload(archiveStream, "source-schema.zip"),
                ApiId = apiId,
                Tag = tag
            };

            var result = await client.UploadFusionSubgraph.ExecuteAsync(input, cancellationToken);

            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.UploadFusionSubgraph.Errors);

            if (data.UploadFusionSubgraph.FusionSubgraphVersion?.Id is null)
            {
                throw new ExitException("Upload of source schema failed!");
            }

            console.Success("Successfully uploaded source schema!");
        }
    }
}
