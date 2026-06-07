using System.Text.Json;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class DownloadClientCommand : Command
{
    public DownloadClientCommand() : base("download")
    {
        Description = "Download the queries from a stage.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<OptionalStageNameOption>.Instance);
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
        var apisClient = services.GetRequiredService<IApisClient>();
        var stagesClient = services.GetRequiredService<IStagesClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        string apiId;
        var apiIdArg = parseResult.GetValue(Opt<OptionalApiIdOption>.Instance);
        if (console.IsInteractive && string.IsNullOrEmpty(apiIdArg))
        {
            apiId = await console.GetOrPromptForApiIdAsync(
                "For which API?", parseResult, apisClient, sessionService, ct);
        }
        else
        {
            apiId = parseResult.GetRequiredOptionalValue(Opt<OptionalApiIdOption>.Instance);
        }

        var stageName = await console.GetOrPromptForStageNameAsync(
            "Which stage?",
            parseResult,
            Opt<OptionalStageNameOption>.Instance,
            stagesClient,
            apiId,
            ct);

        var output = parseResult.GetRequiredValue(Opt<FileSystemOutputOptions>.Instance);
        var format = parseResult.GetRequiredValue(Opt<ClientFormatOption>.Instance);

        if (!Path.IsPathRooted(output))
        {
            output = Path.Combine(fileSystem.GetCurrentDirectory(), output);
        }

        var stream = await client.DownloadPersistedQueriesAsync(apiId, stageName, ct);

        if (stream is null)
        {
            throw new ExitException($"Could not find a published client on stage '{stageName}'.");
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

        console.WriteLine($"Downloaded client to '{output.EscapeMarkup()}'.");

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
