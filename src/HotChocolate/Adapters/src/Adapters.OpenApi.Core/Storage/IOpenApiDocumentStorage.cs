namespace HotChocolate.Adapters.OpenApi;

public interface IOpenApiDocumentStorage : IObservable<OpenApiDefinitionStorageEventArgs>
{
    ValueTask<IEnumerable<IOpenApiDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default);
}
