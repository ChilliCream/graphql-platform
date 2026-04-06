using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class UploadOpenApiCollectionCommand : Command
{
    public UploadOpenApiCollectionCommand() : base("upload")
    {
        Description = "Upload a new OpenAPI collection version.";

        Options.Add(Opt<OpenApiCollectionIdOption>.Instance);
        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<OpenApiCollectionFilePatternOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            openapi upload \
              --openapi-collection-id "<collection-id>" \
              --tag "v1" \
              --pattern "./**/*.graphql"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IOpenApiClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetRequiredValue(Opt<TagOption>.Instance);
        var patterns = parseResult.GetRequiredValue(Opt<OpenApiCollectionFilePatternOption>.Instance);
        var openApiCollectionId = parseResult.GetRequiredValue(Opt<OpenApiCollectionIdOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        var files = fileSystem.GlobMatch(patterns, ["**/bin/**", "**/obj/**"]).ToArray();

        if (files.Length < 1)
        {
            throw new ExitException("Could not find any OpenAPI documents with the provided pattern.");
        }

        var archiveStream =
            await OpenApiCollectionHelpers.BuildOpenApiCollectionArchive(
                fileSystem,
                files,
                cancellationToken);

        await using (var activity = console.StartActivity(
            $"Uploading new OpenAPI collection version '{tag.EscapeMarkup()}' for collection '{openApiCollectionId.EscapeMarkup()}'",
            "Failed to upload a new OpenAPI collection version."))
        {
            var data = await client.UploadOpenApiCollectionVersionAsync(
                openApiCollectionId,
                tag,
                archiveStream,
                source,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IOpenApiCollectionNotFoundError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_InvalidSourceMetadataInputError err => err.Message,
                        IDuplicatedTagError err => err.Message,
                        IConcurrentOperationError err => err.Message,
                        IInvalidOpenApiCollectionArchiveError err => ErrorMessages.InvalidArchive(err.Message),
                        IError err => ErrorMessages.UnexpectedMutationError(err),
                        _ => ErrorMessages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (data.OpenApiCollectionVersion is null)
            {
                activity.Fail();
                console.Error.WriteErrorLine("Could not upload OpenAPI collection version.");
                return ExitCodes.Error;
            }

            activity.Success($"Uploaded new OpenAPI collection version '{tag.EscapeMarkup()}'.");

            return ExitCodes.Success;
        }
    }
}
