using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Adapters.OpenApi.Packaging;
using HotChocolate.Language;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.OpenApi;

internal static class OpenApiCollectionHelpers
{
    public static async Task<MemoryStream> BuildOpenApiCollectionArchive(
        IEnumerable<string> files,
        CancellationToken cancellationToken)
    {
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
        collectionArchive.Dispose();

        return archiveStream;
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
