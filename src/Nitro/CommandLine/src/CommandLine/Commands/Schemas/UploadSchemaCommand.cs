using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
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

        var tag = parseResult.GetRequiredValue(Opt<TagOption>.Instance);
        var schemaFilePath = parseResult.GetRequiredValue(Opt<SchemaFileOption>.Instance);
        var apiId = parseResult.GetRequiredValue(Opt<ApiIdOption>.Instance);
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
                        IUnauthorizedOperation err => err.Message,
                        IInvalidSourceMetadataInputError err => err.Message,
                        IDuplicatedTagError err => err.Message,
                        IConcurrentOperationError err => err.Message,
                        IApiNotFoundError err => err.Message,
                        IError err => ErrorMessages.UnexpectedMutationError(err),
                        _ => ErrorMessages.UnexpectedMutationError()
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

            return ExitCodes.Success;
        }
    }
}
