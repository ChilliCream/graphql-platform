namespace HotChocolate.Adapters.OpenApi;

public interface IOpenApiDefinitionStorage : IObservable<OpenApiDefinitionStorageEventArgs>
{
    ValueTask<IEnumerable<OpenApiDocumentDefinition>> GetDocumentsAsync(CancellationToken cancellationToken = default);
}
