using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class DownloadSchemaCommand : Command
{
    public DownloadSchemaCommand()
        : base("download")
    {
        Description = "Download a schema from a stage";

        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<FileNameOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<IHttpClientFactory>(),
            Opt<ApiIdOption>.Instance,
            Opt<StageNameOption>.Instance,
            Opt<FileNameOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        IHttpClientFactory clientFactory,
        string apiId,
        string stageName,
        FileInfo schemaFile,
        CancellationToken cancellationToken)
    {
        console.Title("Download schema");

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Fetching Schema...", UploadSchema);
        }
        else
        {
            await UploadSchema(null);
        }

        return ExitCodes.Success;

        async Task UploadSchema(StatusContext? ctx)
        {
            using var httpClient = clientFactory.CreateClient(ApiClient.ClientName);

            var encodedApiId = Uri.EscapeDataString(apiId);
            var encodedStageName = Uri.EscapeDataString(stageName);

            using var response = await httpClient.GetAsync(
                $"/api/v1/apis/{encodedApiId}/schemas/latest/download?stage={encodedStageName}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ExitException($"Could not find a published schema on stage {stageName}");
            }

            await using var fileStream = schemaFile.OpenWrite();
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            console.Success($"Downloaded schema to {schemaFile.FullName}");
        }
    }
}
