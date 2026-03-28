using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class UploadSchemaCommand : Command
{
    public UploadSchemaCommand()
        : base("upload")
    {
        Description = "Upload a new schema version";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<SchemaFileOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<OptionalSourceMetadataOption>.Instance);

        this.SetHandler(async context =>
        {
            var console = context.BindingContext.GetRequiredService<INitroConsole>();
            var client = context.BindingContext.GetRequiredService<ISchemasClient>();
            var fileSystem = context.BindingContext.GetRequiredService<IFileSystem>();
            var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;
            var schemaFilePath = context.ParseResult.GetValueForOption(Opt<SchemaFileOption>.Instance)!;
            var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
            var sourceMetadataJson = context.ParseResult.GetValueForOption(Opt<OptionalSourceMetadataOption>.Instance);

            context.ExitCode = await ExecuteAsync(
                console,
                client,
                fileSystem,
                tag,
                schemaFilePath,
                apiId,
                sourceMetadataJson,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        ISchemasClient client,
        IFileSystem fileSystem,
        string tag,
        string schemaFilePath,
        string apiId,
        string? sourceMetadataJson,
        CancellationToken cancellationToken)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var _ = console.StartActivity("Uploading schema..."))
        {
            console.Log("Initialized");
            console.Log($"Reading file [blue]{schemaFilePath.EscapeMarkup()}[/]");

            await using var stream = fileSystem.OpenReadStream(schemaFilePath);

            console.Log("Uploading Schema..");
            await client.UploadSchemaAsync(
                apiId,
                tag,
                stream,
                source,
                cancellationToken);

            console.Success("Successfully uploaded schema!");
        }

        return ExitCodes.Success;
    }
}
