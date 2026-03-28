using ChilliCream.Nitro.Client.Schemas;
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

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<FileNameOption>.Instance);

        this.SetHandler(async context =>
        {
            var console = context.BindingContext.GetRequiredService<INitroConsole>();
            var client = context.BindingContext.GetRequiredService<ISchemasClient>();
            var fileSystem = context.BindingContext.GetRequiredService<IFileSystem>();
            var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
            var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
            var schemaFilePath = context.ParseResult.GetValueForOption(Opt<FileNameOption>.Instance)!;

            context.ExitCode = await ExecuteAsync(
                console,
                client,
                fileSystem,
                apiId,
                stageName,
                schemaFilePath,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        ISchemasClient client,
        IFileSystem fileSystem,
        string apiId,
        string stageName,
        string schemaFilePath,
        CancellationToken cancellationToken)
    {
        await using (var _ = console.StartActivity("Fetching Schema..."))
        {
            await using var schemaStream = await client.DownloadLatestSchemaAsync(
                apiId,
                stageName,
                cancellationToken);

            if (schemaStream is null)
            {
                throw new ExitException($"Could not find a published schema on stage {stageName}");
            }

            if (fileSystem.FileExists(schemaFilePath))
            {
                fileSystem.DeleteFile(schemaFilePath);
            }

            await using var fileStream = fileSystem.CreateFile(schemaFilePath);
            await schemaStream.CopyToAsync(fileStream, cancellationToken);
            console.Success($"Downloaded schema to {schemaFilePath}");
        }

        return ExitCodes.Success;
    }
}
