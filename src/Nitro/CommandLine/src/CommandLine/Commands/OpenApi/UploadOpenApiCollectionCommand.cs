using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class UploadOpenApiCollectionCommand : Command
{
    public UploadOpenApiCollectionCommand() : base("upload")
    {
        Description = "Upload a new OpenAPI collection version";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<OpenApiCollectionIdOption>.Instance);
        AddOption(Opt<OpenApiCollectionFilePatternOption>.Instance);
        AddOption(Opt<OptionalSourceMetadataOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IOpenApiClient>(),
            Bind.FromServiceProvider<IFileSystem>(),
            Opt<TagOption>.Instance,
            Opt<OpenApiCollectionFilePatternOption>.Instance,
            Opt<OpenApiCollectionIdOption>.Instance,
            Opt<OptionalSourceMetadataOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
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
            console.Log("Searching for OpenAPI documents with the following patterns:");
            foreach (var pattern in patterns)
            {
                console.Log($"- {pattern}");
            }

            var files = fileSystem.GlobMatch(patterns, ["**/bin/**", "**/obj/**"]).ToArray();

            if (files.Length < 1)
            {
                console.WriteLine("Could not find any OpenAPI documents with the provided pattern.");
                return;
            }

            var archiveStream =
                await OpenApiCollectionHelpers.BuildOpenApiCollectionArchive(
                    fileSystem,
                    files,
                    cancellationToken);

            console.Log("Uploading OpenAPI collection..");
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
