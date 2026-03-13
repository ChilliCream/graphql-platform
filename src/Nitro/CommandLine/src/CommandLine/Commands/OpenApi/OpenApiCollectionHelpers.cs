using System.Text;
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Adapters.OpenApi.Packaging;
using HotChocolate.Language;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

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
            IOpenApiDefinition definition;
            try
            {
                var fileContent = await File.ReadAllBytesAsync(file, cancellationToken);
                var document = Utf8GraphQLParser.Parse(fileContent);
                definition = OpenApiDefinitionParser.Parse(document);
            }
            catch (Exception exception)
            {
                throw new ExitException($"Encountered an error while trying to parse '{file}': {exception.Message}");
            }

            if (definition is OpenApiEndpointDefinition endpoint)
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
            else if (definition is OpenApiModelDefinition model)
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

        archiveStream.Position = 0;

        return archiveStream;
    }
}
