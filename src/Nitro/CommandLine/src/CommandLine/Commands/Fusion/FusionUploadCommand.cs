using System.Collections.Immutable;
using System.Text;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.CommandLineResources;

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

        var sourceSchemaFileOption = new Option<List<string>>("--source-schema-file")
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
            var sourceSchemaFiles = context.ParseResult.GetValueForOption(sourceSchemaFileOption)!;
            var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
            var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;

            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            var apiClient = context.BindingContext.GetRequiredService<IApiClient>();

            context.ExitCode = await ExecuteAsync(
                console,
                apiClient,
                workingDirectory,
                sourceSchemaFiles,
                tag,
                apiId,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        string tag,
        string apiId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(workingDirectory))
        {
            throw new ExitException("Expected a non-empty value for the '--working-directory'.");
        }

        console.Title("Upload source schemas");

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Uploading source schemas...", UploadSourceSchemaFiles);
        }
        else
        {
            await UploadSourceSchemaFiles(null);
        }

        return ExitCodes.Success;

        async Task UploadSourceSchemaFiles(StatusContext? ctx)
        {
            if (sourceSchemaFiles.Count == 0)
            {
                // TODO: In this case there can only ever be one source schema file, since
                //       the name schema-settings.json can only be used once in the directory.
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

            var sourceSchemas = await FusionComposeCommand.ReadSourceSchemasAsync(
                sourceSchemaFiles,
                cancellationToken);

            await using var archiveStream =  new MemoryStream();
            var archive = FusionArchive.Create(archiveStream, leaveOpen: true);

            await archive.SetArchiveMetadataAsync(
                new ArchiveMetadata
                {
                    // TODO: This is just here to satisfy the initializer
                    SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
                    SourceSchemas = [..sourceSchemas.Keys]
                },
                cancellationToken);

            foreach (var (schemaName, (sourceSchema, schemaSettings)) in sourceSchemas)
            {
                await archive.SetSourceSchemaConfigurationAsync(
                    schemaName,
                    Encoding.UTF8.GetBytes(sourceSchema.SourceText),
                    schemaSettings,
                    cancellationToken);
            }

            await archive.CommitAsync(cancellationToken);
            archive.Dispose();

            archiveStream.Seek(0, SeekOrigin.Begin);

            await File.WriteAllBytesAsync(
                "/Users/tobiastengler/src/graphql-platform/account/test.far",
                archiveStream.ToArray(),
                cancellationToken);

            // var input = new UploadClientInput
            // {
            //     Operations = new Upload(stream, "archive.far"),
            //     ClientId = apiId,
            //     Tag = tag
            // };
            //
            // // TODO: Use new mutation once available
            // var result = await client.UploadClient.ExecuteAsync(input, cancellationToken);
            //
            // console.EnsureNoErrors(result);
            // var data = console.EnsureData(result);
            // console.PrintErrorsAndExit(data.UploadClient.Errors);
            //
            // if (data.UploadClient.ClientVersion?.Id is null)
            // {
            //     throw new ExitException("Upload operations failed!");
            // }

            console.Success("Successfully uploaded source schemas!");
        }
    }
}
