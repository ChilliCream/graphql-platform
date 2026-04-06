using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class DownloadSchemaCommand : Command
{
    public DownloadSchemaCommand() : base("download")
    {
        Description = "Download a schema from a stage.";

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<FileNameOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            schema download \
              --api-id "<api-id>" \
              --stage "dev" \
              --file ./schema.graphqls
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<ISchemasClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var apiId = parseResult.GetRequiredValue(Opt<ApiIdOption>.Instance);
        var stageName = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var schemaFilePath = parseResult.GetRequiredValue(Opt<FileNameOption>.Instance);

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

            return ExitCodes.Success;
        }
    }
}
