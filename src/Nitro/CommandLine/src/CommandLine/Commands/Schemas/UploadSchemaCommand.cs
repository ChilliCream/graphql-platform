using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class UploadSchemaCommand : Command
{
    public UploadSchemaCommand() : base("upload")
    {
        Description = "Upload a new schema version.";

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<SchemaFileOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            schema upload \
              --api-id "<api-id>" \
              --tag "v1" \
              --schema-file ./schema.graphqls
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
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var schemaFilePath = parseResult.GetValue(Opt<SchemaFileOption>.Instance)!;
        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity(
            $"Uploading new schema version '{tag.EscapeMarkup()}' to API '{apiId.EscapeMarkup()}'",
            "Failed to upload a new schema version."))
        {
            await using var stream = fileSystem.OpenReadStream(schemaFilePath);

            var data = await client.UploadSchemaAsync(
                apiId,
                tag,
                stream,
                source,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IUploadSchema_UploadSchema_Errors_UnauthorizedOperation err => err.Message,
                        IUploadSchema_UploadSchema_Errors_DuplicatedTagError err => err.Message,
                        IUploadSchema_UploadSchema_Errors_ConcurrentOperationError err => err.Message,
                        IUploadSchema_UploadSchema_Errors_ApiNotFoundError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (data.SchemaVersion is null)
            {
                throw new ExitException("Could not upload schema.");
            }

            activity.Success($"Uploaded new schema version '{tag.EscapeMarkup()}'.");

            if (!console.IsHumanReadable)
            {
                resultHolder.SetResult(new ObjectResult(new UploadSchemaResult
                {
                    SchemaVersionId = data.SchemaVersion.Id,
                    Tag = tag
                }));
            }

            return ExitCodes.Success;
        }
    }

    public class UploadSchemaResult
    {
        public required string SchemaVersionId { get; init; }

        public required string Tag { get; init; }
    }
}
