namespace HotChocolate.Exporters.OpenApi;

// TODO: Different name
public interface IOpenApiDefinitionStorage : IObservable<OpenApiDefinitionStorageEventArgs>
{
    ValueTask<IEnumerable<OpenApiDocumentDefinition>> GetDocumentsAsync(CancellationToken cancellationToken = default);
}
