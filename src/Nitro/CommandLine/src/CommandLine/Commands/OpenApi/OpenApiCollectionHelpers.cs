using System.Text;
using ChilliCream.Nitro.Adapters.OpenApi.Extensions;
using ChilliCream.Nitro.Adapters.OpenApi.Packaging;
using ChilliCream.Nitro.Adapters.OpenApi.Serialization;
using ChilliCream.Nitro.CommandLine.Services;
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Language;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal static class OpenApiCollectionHelpers
{
    public static async Task<MemoryStream> BuildOpenApiCollectionArchive(
        IFileSystem fileSystem,
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
                var fileContent = await fileSystem.ReadAllBytesAsync(file, cancellationToken);
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
                var settings = endpoint.ToSettings();
                var settingsJson = OpenApiEndpointSettingsSerializer.Format(settings);

                await collectionArchive.AddOpenApiEndpointAsync(
                    endpointKey,
                    documentBytes,
                    settingsJson,
                    cancellationToken);
            }
            else if (definition is OpenApiModelDefinition model)
            {
                var documentBytes = Encoding.UTF8.GetBytes(model.Document.ToString());
                var settings = model.ToSettings();
                var settingsJson = OpenApiModelSettingsSerializer.Format(settings);

                await collectionArchive.AddOpenApiModelAsync(
                    model.Name,
                    documentBytes,
                    settingsJson,
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
