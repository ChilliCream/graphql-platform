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

        foreach (var file in files)
        {
            var fileContent = await File.ReadAllBytesAsync(file, cancellationToken);
            var document = Utf8GraphQLParser.Parse(fileContent);
            var parseResult = OpenApiDefinitionParser.Parse(document);

            if (!parseResult.IsValid)
            {
                // TODO: Handle properly
                continue;
            }

            if (parseResult.Definition is OpenApiEndpointDefinition endpoint)
            {
                var endpointKey = new OpenApiEndpointKey(endpoint.HttpMethod, endpoint.Route);
                var documentBytes = Encoding.UTF8.GetBytes(endpoint.Document.ToString());
                var settingsDto = endpoint.ToDto();
                var settings = OpenApiEndpointSettingsSerializer.Format(settingsDto);

                await collectionArchive.AddOpenApiEndpointAsync(
                    endpointKey,
                    documentBytes,
                    settings,
                    cancellationToken);
            }
            else if (parseResult.Definition is OpenApiModelDefinition model)
            {
                var documentBytes = Encoding.UTF8.GetBytes(model.Document.ToString());
                var settingsDto = model.ToDto();
                var settings = OpenApiModelSettingsSerializer.Format(settingsDto);

                await collectionArchive.AddOpenApiModelAsync(
                    model.Name,
                    documentBytes,
                    settings,
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
}
