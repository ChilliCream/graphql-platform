using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class DownloadSchemaCommand : Command
{
    public DownloadSchemaCommand(
        INitroConsole console,
        ISchemasClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder)
        : base("download")
    {
        Description = "Download a schema from a stage";

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<FileNameOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                client,
                fileSystem,
                sessionService,
                resultHolder,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        ISchemasClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var stageName = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var schemaFilePath = parseResult.GetValue(Opt<FileNameOption>.Instance)!;

        await using (var activity = console.StartActivity($"Downloading schema from stage '{stageName.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}'", "Failed to download the schema."))
        {
            await using var schemaStream = await client.DownloadLatestSchemaAsync(
                apiId,
                stageName,
                cancellationToken);

            if (schemaStream is null)
            {
                throw Exit($"Could not find a published schema on stage '{stageName}'.");
            }

            if (fileSystem.FileExists(schemaFilePath))
            {
                fileSystem.DeleteFile(schemaFilePath);
            }

            await using var fileStream = fileSystem.CreateFile(schemaFilePath);
            await schemaStream.CopyToAsync(fileStream, cancellationToken);

            activity.Success($"Downloaded the schema from stage '{stageName.EscapeMarkup()}'.");

            if (!console.IsHumanReadable)
            {
                resultHolder.SetResult(new ObjectResult(new DownloadSchemaResult
                {
                    File = schemaFilePath
                }));
            }

            return ExitCodes.Success;
        }
    }

    public class DownloadSchemaResult
    {
        public required string File { get; init; }
    }
}
