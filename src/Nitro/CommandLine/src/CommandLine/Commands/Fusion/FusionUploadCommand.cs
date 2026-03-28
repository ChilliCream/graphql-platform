#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;
using HotChocolate.Fusion.SourceSchema.Packaging;
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

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<SourceSchemaFileOption>.Instance);
        Options.Add(Opt<WorkingDirectoryOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);
        this.AddGlobalNitroOptions();

        this.SetHandler(async context =>
        {
            var workingDirectory = context.ParseResult.GetValueForOption(Opt<WorkingDirectoryOption>.Instance)!;
            var sourceSchemaFile = context.ParseResult.GetValueForOption(Opt<SourceSchemaFileOption>.Instance)!;
            var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
            var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;
            var sourceMetadataJson = context.ParseResult.GetValueForOption(Opt<OptionalSourceMetadataOption>.Instance);

            var console = context.BindingContext.GetRequiredService<INitroConsole>();
            var fusionConfigurationClient =
                context.BindingContext.GetRequiredService<IFusionConfigurationClient>();
            var fileSystem = context.BindingContext.GetRequiredService<IFileSystem>();

            context.ExitCode = await ExecuteAsync(
                console,
                fusionConfigurationClient,
                workingDirectory,
                sourceSchemaFile,
                tag,
                apiId,
                sourceMetadataJson,
                fileSystem,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        string workingDirectory,
        string sourceSchemaFilePath,
        string tag,
        string apiId,
        string? sourceMetadataJson,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var _ = console.StartActivity("Uploading source schema..."))
        {
            await UploadSourceSchemaFile();
        }

        return ExitCodes.Success;

        async Task UploadSourceSchemaFile()
        {
            if (!Path.IsPathRooted(sourceSchemaFilePath))
            {
                sourceSchemaFilePath = Path.Combine(workingDirectory, sourceSchemaFilePath);
            }

            var (_, sourceText, settings) = await FusionComposeCommand.ReadSourceSchemaAsync(
                fileSystem,
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

            var result = await fusionConfigurationClient.UploadFusionSubgraphAsync(
                apiId,
                tag,
                archiveStream,
                source,
                cancellationToken);
            console.PrintMutationErrorsAndExit(result.Errors);

            if (string.IsNullOrWhiteSpace(result.FusionSubgraphVersion?.Id))
            {
                throw new ExitException("Upload of source schema failed!");
            }

            console.Success("Successfully uploaded source schema!");
        }
    }
}
