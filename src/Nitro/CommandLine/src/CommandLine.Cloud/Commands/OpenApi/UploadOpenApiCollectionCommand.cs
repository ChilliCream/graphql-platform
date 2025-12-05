using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Helpers;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using StrawberryShake;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class UploadOpenApiCollectionCommand : Command
{
    public UploadOpenApiCollectionCommand() : base("upload")
    {
        Description = "Upload a new OpenAPI collection version";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<OpenApiCollectionFileOption>.Instance);
        AddOption(Opt<OpenApiCollectionIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<TagOption>.Instance,
            Opt<OpenApiCollectionFileOption>.Instance,
            Opt<OpenApiCollectionIdOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string tag,
        FileInfo operationsFile,
        string openApiCollectionId,
        CancellationToken cancellationToken)
    {
        console.Title($"Uploading OpenAPI collection {operationsFile.FullName.EscapeMarkup()}");

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Uploading OpenAPI collection...", UploadOpenApiCollection);
        }
        else
        {
            await UploadOpenApiCollection(null);
        }

        return ExitCodes.Success;

        async Task UploadOpenApiCollection(StatusContext? ctx)
        {
            console.Log("Initialized");
            console.Log($"Reading file [blue]{operationsFile.FullName.EscapeMarkup()}[/]");

            var stream = FileHelpers.CreateFileStream(operationsFile);

            var input = new UploadOpenApiCollectionInput
            {
                Collection = new Upload(stream, "operations.graphql"),
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
                throw new ExitException("Upload of OpenAPI collection failed!");
            }

            console.Success("Successfully uploaded OpenAPI collection!");
        }
    }
}
