using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Adapters.OpenApi.Packaging;
using HotChocolate.Language;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using StrawberryShake;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class UploadOpenApiCollectionCommand : Command
{
    public UploadOpenApiCollectionCommand() : base("upload")
    {
        Description = "Upload a new OpenAPI collection version";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<OpenApiCollectionIdOption>.Instance);

        var patternsOption = new Option<List<string>>("--patterns")
        {
            Description = "TODO"
        };
        patternsOption.AddAlias("-p");

        AddOption(patternsOption);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<TagOption>.Instance,
            patternsOption,
            Opt<OpenApiCollectionIdOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string tag,
        List<string> patterns,
        string openApiCollectionId,
        CancellationToken cancellationToken)
    {
        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Uploading new OpenAPI collection version...", UploadOpenApiCollection);
        }
        else
        {
            await UploadOpenApiCollection(null);
        }

        return ExitCodes.Success;

        async Task UploadOpenApiCollection(StatusContext? ctx)
        {
            // TODO: Print patterns for confirmation

            var files = GlobMatcher.Match(patterns).ToArray();

            if (files.Length < 1)
            {
                // TODO: Improve this error
                console.ErrorLine("Did not find any matches...");
                return;
            }

            var archiveStream = new MemoryStream();
            var collectionArchive = OpenApiCollectionArchive.Create(archiveStream, leaveOpen: true);

            await collectionArchive.SetArchiveMetadataAsync(
                new ArchiveMetadata(),
                cancellationToken);

            var parser = new OpenApiDocumentParser();

            foreach (var file in files)
            {
                var fileContent = await File.ReadAllBytesAsync(file, cancellationToken);
                var document = Utf8GraphQLParser.Parse(fileContent);
                // TODO: The id doesn't mean anything, we should probably get rid of it...
                var openApiDocumentDefinition = new OpenApiDocumentDefinition(file, document);

                var parseResult = parser.Parse(openApiDocumentDefinition);

                if (!parseResult.IsValid)
                {
                    // TODO: Handle properly
                    continue;
                }

                if (parseResult.Document is OpenApiOperationDocument operationDocument)
                {
                    var operationBytes = Encoding.UTF8.GetBytes(operationDocument.OperationDefinition.ToString());
                    var settings = CreateJsonSettingsForOperationDocument(operationDocument);

                    await collectionArchive.AddOpenApiEndpointAsync(
                        operationDocument.Name,
                        operationBytes,
                        settings,
                        cancellationToken);
                }
                else if (parseResult.Document is OpenApiFragmentDocument fragmentDocument)
                {
                    var fragmentBytes = Encoding.UTF8.GetBytes(fragmentDocument.FragmentDefinition.ToString());

                    await collectionArchive.AddOpenApiModelAsync(
                        fragmentDocument.Name,
                        fragmentBytes,
                        cancellationToken);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            await collectionArchive.CommitAsync(cancellationToken);

            var input = new UploadOpenApiCollectionInput
            {
                Collection = new Upload(archiveStream, "collection.zip"),
                OpenApiCollectionId = openApiCollectionId,
                Tag = tag
            };

            console.Log("Uploading OpenAPI collection..");
            var result = await client.UploadOpenApiCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.UploadOpenApiCollection.Errors);

            if (data.UploadOpenApiCollection.OpenApiCollectionVersion?.Id is null)
            {
                throw new ExitException("Upload of OpenAPI collection failed!");
            }

            console.Success("Successfully uploaded new OpenAPI collection version!");
        }
    }

    private static JsonDocument CreateJsonSettingsForOperationDocument(OpenApiOperationDocument document)
    {
        var obj = new JsonObject();
        obj.Add("httpMethod", document.HttpMethod);
        obj.Add("route", document.Route.ToOpenApiPath());

        // TODO: Add other settings

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            obj.WriteTo(writer);
        }

        stream.Position = 0;

        return JsonDocument.Parse(stream);
    }
}
