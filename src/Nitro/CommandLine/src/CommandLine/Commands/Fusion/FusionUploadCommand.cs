using System.Collections.Immutable;
using System.Text;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.SourceSchema.Packaging;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.CommandLineResources;
using ArchiveMetadata = HotChocolate.Fusion.SourceSchema.Packaging.ArchiveMetadata;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

public sealed class FusionUploadCommand : Command
{
    public FusionUploadCommand() : base("upload")
    {
        Description = "Upload source schemas for a new Fusion configuration version";

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
            throw new ExitException("Expected a non-empty value for the '--working-directory'.");
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

            var sourceSchemas = await FusionComposeCommand.ReadSourceSchemasAsync(
                [sourceSchemaFilePath],
                cancellationToken);
            var sourceSchema = sourceSchemas.First().Value;

            await using var archiveStream = new MemoryStream();
            var archive = FusionSourceSchemaArchive.Create(archiveStream, leaveOpen: true);

            await archive.SetArchiveMetadataAsync(new ArchiveMetadata(), cancellationToken);
            await archive.SetSchemaAsync(
                Encoding.UTF8.GetBytes(sourceSchema.Item1.SourceText),
                cancellationToken);
            await archive.SetSettingsAsync(sourceSchema.Item2, cancellationToken);

            await archive.CommitAsync(cancellationToken);
            archive.Dispose();

            archiveStream.Position = 0;

            // TODO: Use new mutation once available
            var input = new UploadClientInput
            {
                Operations = new Upload(archiveStream, "source-schema.zip"),
                ClientId = apiId,
                Tag = tag
            };

            var result = await client.UploadClient.ExecuteAsync(input, cancellationToken);

            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.UploadClient.Errors);

            if (data.UploadClient.ClientVersion?.Id is null)
            {
                throw new ExitException("Upload of source schema failed!");
            }

            console.Success("Successfully uploaded source schema!");
        }
    }
}
