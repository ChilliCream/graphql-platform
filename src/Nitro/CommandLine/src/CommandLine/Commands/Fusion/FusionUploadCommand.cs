#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using HotChocolate.Fusion.SourceSchema.Packaging;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;
using ArchiveMetadata = HotChocolate.Fusion.SourceSchema.Packaging.ArchiveMetadata;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionUploadCommand : Command
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

        this.SetActionWithExceptionHandling(async (services, parseResult, cancellationToken) =>
        {
            var console = services.GetRequiredService<INitroConsole>();
            var fusionConfigurationClient = services.GetRequiredService<IFusionConfigurationClient>();
            var fileSystem = services.GetRequiredService<IFileSystem>();
            var sessionService = services.GetRequiredService<ISessionService>();
            var resultHolder = services.GetRequiredService<IResultHolder>();
            return await ExecuteAsync(
                parseResult,
                console,
                fusionConfigurationClient,
                fileSystem,
                sessionService,
                resultHolder,
                cancellationToken);
        });
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var workingDirectory = parseResult.GetValue(Opt<WorkingDirectoryOption>.Instance)
            ?? fileSystem.GetCurrentDirectory();
        var sourceSchemaFile = parseResult.GetValue(Opt<SourceSchemaFileOption>.Instance)!;
        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        if (!Path.IsPathRooted(sourceSchemaFile))
        {
            sourceSchemaFile = Path.Combine(workingDirectory, sourceSchemaFile);
        }

        var (_, sourceText, settings) = await FusionComposeCommand.ReadSourceSchemaAsync(
            fileSystem,
            sourceSchemaFile,
            cancellationToken);

        await using (var activity = console.StartActivity(
            $"Uploading new source schema version '{tag.EscapeMarkup()}' to API '{apiId.EscapeMarkup()}'",
            "Failed to upload a new source schema version."))
        {
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

            if (result.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in result.Errors)
                {
                    var errorMessage = error switch
                    {
                        IUnauthorizedOperation err => err.Message,
                        IDuplicatedTagError err => err.Message,
                        IConcurrentOperationError err => err.Message,
                        IInvalidFusionSourceSchemaArchiveError err =>
                            "The server received an invalid archive. "
                            + "This indicates a bug in the tooling. "
                            + "Please notify ChilliCream."
                            + "Error received: "
                            + err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (string.IsNullOrWhiteSpace(result.FusionSubgraphVersion?.Id))
            {
                throw Exit("Upload of source schema failed.");
            }

            activity.Success($"Uploaded new source schema version '{tag.EscapeMarkup()}'.");

            if (!console.IsHumanReadable)
            {
                resultHolder.SetResult(new ObjectResult(new FusionUploadResult
                {
                    Tag = tag
                }));
            }

            return ExitCodes.Success;
        }
    }

    public class FusionUploadResult
    {
        public required string Tag { get; init; }
    }
}
