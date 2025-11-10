namespace HotChocolate.Exporters.OpenApi;

public interface IOpenApiDefinitionStorage : IObservable<OpenApiDefinitionStorageEventArgs>
{
    ValueTask<IEnumerable<OpenApiDocumentDefinition>> GetDocumentsAsync(CancellationToken cancellationToken = default);
}
