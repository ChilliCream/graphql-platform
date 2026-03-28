using System.Text.Json;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Configuration;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class DownloadClientCommand : Command
{
    public DownloadClientCommand(
        INitroConsole console,
        IClientsClient client,
        IFileSystem fileSystem) : base("download")
    {
        Description = "Download the queries from a stage";

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<FileSystemOutputOptions>.Instance);
        Options.Add(Opt<ClientFormatOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(
                console,
                client,
                fileSystem,
                parseResult.GetValue(Opt<ApiIdOption>.Instance)!,
                parseResult.GetValue(Opt<StageNameOption>.Instance)!,
                parseResult.GetValue(Opt<FileSystemOutputOptions>.Instance)!,
                parseResult.GetValue(Opt<ClientFormatOption>.Instance)!,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        IClientsClient client,
        IFileSystem fileSystem,
        string apiId,
        string stageName,
        string output,
        string format,
        CancellationToken ct)
    {
        await using (var _ = console.StartActivity("Fetching queries..."))
        {
            var stream = await client.DownloadPersistedQueriesAsync(apiId, stageName, ct);

            if (stream is null)
            {
                throw new ExitException($"Could not find a published client on stage {stageName}");
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
                        await WriteToFolder(console, fileSystem, output, queries);
                        break;

                    case ClientFormat.Relay:
                        await WriteToRelayJson(fileSystem, output, queries);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(format), format, null);
                }
            }
        }

        return ExitCodes.Success;
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
        INitroConsole console,
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

        console.Success($"Downloaded client to {outputPath}");
    }
}

internal sealed class PersistedQueryStreamResult
{
    public Guid ApiId { get; init; } = default!;

    public string[] DocumentIds { get; init; } = default!;

    public string Content { get; set; } = default!;
}
