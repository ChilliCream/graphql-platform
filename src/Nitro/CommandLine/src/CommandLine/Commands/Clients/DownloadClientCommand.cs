using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Configuration;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class DownloadClientCommand : Command
{
    public DownloadClientCommand()
        : base("download")
    {
        Description = "Download the queries from a stage";

        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<FileSystemOutputOptions>.Instance);
        AddOption(Opt<ClientFormatOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<IHttpClientFactory>(),
            Opt<ApiIdOption>.Instance,
            Opt<StageNameOption>.Instance,
            Opt<FileSystemOutputOptions>.Instance,
            Opt<ClientFormatOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        IHttpClientFactory clientFactory,
        string apiId,
        string stageName,
        FileSystemInfo output,
        string format,
        CancellationToken ct)
    {
        console.Title("Download persisted queries");

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Fetching Queries...", UploadClient);
        }
        else
        {
            await UploadClient(null);
        }

        return ExitCodes.Success;

        async Task UploadClient(StatusContext? ctx)
        {
            using var httpClient = clientFactory.CreateClient(ApiClient.ClientName);

            var encodedApiId = Uri.EscapeDataString(apiId);
            var encodedStageName = Uri.EscapeDataString(stageName);

            using var response = await httpClient.GetAsync(
                $"/api/v1/apis/{encodedApiId}/persistedQueries?stage={encodedStageName}",
                ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new ExitException($"Could not find a published client on stage {stageName}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var queries = JsonSerializer
                .DeserializeAsyncEnumerable(stream, NitroCLIJsonContext.Default.PersistedQueryStreamResult, ct);

            switch (format)
            {
                case ClientFormat.Folder:
                    await WriteToFolder(console, output, queries);
                    break;

                case ClientFormat.Relay:
                    await WriteToRelayJson(output, queries);
                    break;
            }
        }
    }

    private static async Task WriteToRelayJson(
        FileSystemInfo output,
        IAsyncEnumerable<PersistedQueryStreamResult?> queries)
    {
        if (File.Exists(output.FullName))
        {
            File.Delete(output.FullName);
        }

        await using var fileStream = File.OpenWrite(output.FullName);
        await using var utf8Writer = new Utf8JsonWriter(fileStream);
        utf8Writer.WriteStartObject();
        await foreach (var query in queries)
        {
            if (query is null)
            {
                continue;
            }

            foreach (var documentId in query.DocumentIds)
            {
                utf8Writer.WriteString(documentId, query.Content);
            }
        }

        utf8Writer.WriteEndObject();
    }

    private static async Task WriteToFolder(
        IAnsiConsole console,
        FileSystemInfo output,
        IAsyncEnumerable<PersistedQueryStreamResult?> queries)
    {
        if (!output.Exists)
        {
            Directory.CreateDirectory(output.FullName);
        }

        await foreach (var query in queries)
        {
            if (query is null)
            {
                continue;
            }

            foreach (var documentId in query.DocumentIds)
            {
                var path = Path.Combine(output.FullName, $"{documentId}.graphql");
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }

                await using var fileStream = fileInfo.OpenWrite();
                await using var writer = new StreamWriter(fileStream);
                await writer.WriteAsync(query.Content);
            }
        }

        console.Success($"Downloaded client to {output.FullName}");
    }
}

internal sealed class PersistedQueryStreamResult
{
    public Guid ApiId { get; init; } = default!;

    public string[] DocumentIds { get; init; } = default!;

    public string Content { get; set; } = default!;
}
