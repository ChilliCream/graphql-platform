#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using HotChocolate.Fusion.SourceSchema.Packaging;
using StrawberryShake;
using ArchiveMetadata = HotChocolate.Fusion.SourceSchema.Packaging.ArchiveMetadata;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public sealed class FusionUploadCommand : Command
{
    public FusionUploadCommand() : base("upload")
    {
        Description = "Upload a source schema for a later composition.";

        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<SourceSchemaFileOption>.Instance);
        AddOption(Opt<WorkingDirectoryOption>.Instance);
        this.AddNitroCloudDefaultOptions();

        this.SetHandler(async context =>
        {
            var workingDirectory = context.ParseResult.GetValueForOption(Opt<WorkingDirectoryOption>.Instance)!;
            var sourceSchemaFile = context.ParseResult.GetValueForOption(Opt<SourceSchemaFileOption>.Instance)!;
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
        string sourceSchemaFilePath,
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
