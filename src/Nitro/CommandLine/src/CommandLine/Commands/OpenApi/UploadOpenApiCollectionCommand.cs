using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class UploadOpenApiCollectionCommand : Command
{
    public UploadOpenApiCollectionCommand(
        INitroConsole console,
        IOpenApiClient client,
        IFileSystem fileSystem,
        ISessionService sessionService) : base("upload")
    {
        Description = "Upload a new OpenAPI collection version";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<OpenApiCollectionIdOption>.Instance);
        Options.Add(Opt<OpenApiCollectionFilePatternOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, fileSystem, sessionService, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IOpenApiClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var patterns = parseResult.GetValue(Opt<OpenApiCollectionFilePatternOption>.Instance)!;
        var openApiCollectionId = parseResult.GetValue(Opt<OpenApiCollectionIdOption>.Instance)!;
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
                        IDuplicatedTagError err => err.Message,
                        IConcurrentOperationError err => err.Message,
                        IInvalidOpenApiCollectionArchiveError err => ErrorMessages.InvalidArchive(err.Message),
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
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
