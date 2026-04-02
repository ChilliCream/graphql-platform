using System.Text.Json;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class DownloadClientCommand : Command
{
    public DownloadClientCommand() : base("download")
    {
        Description = "Download the queries from a stage.";

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<FileSystemOutputOptions>.Instance);
        Options.Add(Opt<ClientFormatOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            client download \
              --api-id "<api-id>" \
              --stage "dev" \
              --path ./operations.json
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IClientsClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var stageName = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var output = parseResult.GetValue(Opt<FileSystemOutputOptions>.Instance)!;
        var format = parseResult.GetValue(Opt<ClientFormatOption>.Instance)!;

        await using (var activity = console.StartActivity(
            $"Downloading client from stage '{stageName.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}'",
            "Failed to download the client."))
        {
            var stream = await client.DownloadPersistedQueriesAsync(apiId, stageName, ct);

            if (stream is null)
            {
                throw Exit($"Could not find a published client on stage '{stageName}'.");
            }

            await using (stream)
            {
                var queries = JsonSerializer.DeserializeAsyncEnumerable(
                    stream,
                    NitroCLIJsonContext.Default.PersistedQueryStreamResult,
                    ct);

                switch (format)
                {
                    case ClientFormat.Folder:
                        await WriteToFolder(fileSystem, output, queries);
                        break;

                    case ClientFormat.Relay:
                        await WriteToRelayJson(fileSystem, output, queries);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(format), format, null);
                }
            }

            activity.Success($"Downloaded the client from stage '{stageName.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(new DownloadClientResult
            {
                File = output,
                Format = format.ToLowerInvariant()
            }));

            return ExitCodes.Success;
        }
    }

    public class DownloadClientResult
    {
        public required string File { get; init; }

        public required string Format { get; init; }
    }

    private static async Task WriteToRelayJson(
        IFileSystem fileSystem,
        string outputPath,
        IAsyncEnumerable<PersistedQueryStreamResult?> queries)
    {
        if (fileSystem.FileExists(outputPath))
        {
            fileSystem.DeleteFile(outputPath);
        }

        await using var fileStream = fileSystem.CreateFile(outputPath);
        await using var utf8Writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = true });
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
        IFileSystem fileSystem,
        string outputPath,
        IAsyncEnumerable<PersistedQueryStreamResult?> queries)
    {
        if (!fileSystem.DirectoryExists(outputPath))
        {
            fileSystem.CreateDirectory(outputPath);
        }

        await foreach (var query in queries)
        {
            if (query is null)
            {
                continue;
            }

            foreach (var documentId in query.DocumentIds)
            {
                var path = Path.Combine(outputPath, $"{documentId}.graphql");
                if (fileSystem.FileExists(path))
                {
                    fileSystem.DeleteFile(path);
                }

                await using var fileStream = fileSystem.CreateFile(path);
                await using var writer = new StreamWriter(fileStream);
                await writer.WriteAsync(query.Content);
            }
        }
    }
}

internal sealed class PersistedQueryStreamResult
{
    public Guid ApiId { get; init; } = default!;

    public string[] DocumentIds { get; init; } = default!;

    public string Content { get; set; } = default!;
}
