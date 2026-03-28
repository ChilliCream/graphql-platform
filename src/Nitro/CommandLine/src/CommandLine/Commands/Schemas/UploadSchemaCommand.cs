using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class UploadSchemaCommand : Command
{
    public UploadSchemaCommand(
        INitroConsole console,
        ISchemasClient client,
        IFileSystem fileSystem)
        : base("upload")
    {
        Description = "Upload a new schema version";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<SchemaFileOption>.Instance);
        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(
                console,
                client,
                fileSystem,
                parseResult.GetValue(Opt<TagOption>.Instance)!,
                parseResult.GetValue(Opt<SchemaFileOption>.Instance)!,
                parseResult.GetValue(Opt<ApiIdOption>.Instance)!,
                parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance),
                cancellationToken));
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
