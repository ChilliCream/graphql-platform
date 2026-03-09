using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using StrawberryShake;
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

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<TagOption>.Instance,
            Opt<OpenApiCollectionFilePatternOption>.Instance,
            Opt<OpenApiCollectionIdOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string tag,
        List<string> patterns,
        string openApiCollectionId,
        CancellationToken cancellationToken)
    {
        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Uploading new OpenAPI collection version...", UploadOpenApiCollection);
        }
        else
        {
            await UploadOpenApiCollection(null);
        }

        return ExitCodes.Success;

        async Task UploadOpenApiCollection(StatusContext? ctx)
        {
            console.Log("Searching for OpenAPI documents with the following patterns:");
            foreach (var pattern in patterns)
            {
                console.Log($"- {pattern}");
            }

            var files = GlobMatcher.Match(patterns).ToArray();

            if (files.Length < 1)
            {
                console.WriteLine("Could not find any OpenAPI documents with the provided pattern.");
                return;
            }

            var archiveStream =
                await OpenApiCollectionHelpers.BuildOpenApiCollectionArchive(files, cancellationToken);

            var input = new UploadOpenApiCollectionInput
            {
                Collection = new Upload(archiveStream, "collection.zip"),
                OpenApiCollectionId = openApiCollectionId,
                Tag = tag
            };

            console.Log("Uploading OpenAPI collection..");
            var result = await client.UploadOpenApiCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.UploadOpenApiCollection.Errors);

            if (data.UploadOpenApiCollection.OpenApiCollectionVersion?.Id is null)
            {
                throw new ExitException("Upload of OpenAPI collection failed.");
            }

            console.Success("Successfully uploaded new OpenAPI collection version!");
        }
    }
}
