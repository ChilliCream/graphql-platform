using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class UploadOpenApiCollectionCommand : Command
{
    public UploadOpenApiCollectionCommand(
        INitroConsole console,
        IOpenApiClient client,
        IFileSystem fileSystem) : base("upload")
    {
        Description = "Upload a new OpenAPI collection version";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<OpenApiCollectionIdOption>.Instance);
        Options.Add(Opt<OpenApiCollectionFilePatternOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                console,
                client,
                fileSystem,
                parseResult.GetValue(Opt<TagOption>.Instance)!,
                parseResult.GetValue(Opt<OpenApiCollectionFilePatternOption>.Instance)!,
                parseResult.GetValue(Opt<OpenApiCollectionIdOption>.Instance)!,
                parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance),
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        IOpenApiClient client,
        IFileSystem fileSystem,
        string tag,
        List<string> patterns,
        string openApiCollectionId,
        string? sourceMetadataJson,
        CancellationToken cancellationToken)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var _ = console.StartActivity("Uploading new OpenAPI collection version..."))
        {
            await UploadOpenApiCollection();
        }

        return ExitCodes.Success;

        async Task UploadOpenApiCollection()
        {
            // console.Log("Searching for OpenAPI documents with the following patterns:");
            // foreach (var pattern in patterns)
            // {
            //     console.Log($"- {pattern}");
            // }

            var files = fileSystem.GlobMatch(patterns, ["**/bin/**", "**/obj/**"]).ToArray();

            if (files.Length < 1)
            {
                throw new ExitException("Could not find any OpenAPI documents with the provided pattern.");
            }

            var archiveStream =
                await OpenApiCollectionHelpers.BuildOpenApiCollectionArchive(
                    fileSystem,
                    files,
                    cancellationToken);

            await client.UploadOpenApiCollectionVersionAsync(
                openApiCollectionId,
                tag,
                archiveStream,
                source,
                cancellationToken);

            console.Success("Successfully uploaded new OpenAPI collection version!");
        }
    }
}
