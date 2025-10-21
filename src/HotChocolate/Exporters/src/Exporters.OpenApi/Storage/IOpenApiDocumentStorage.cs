namespace HotChocolate.Exporters.OpenApi;

// TODO: Different name
public interface IOpenApiDocumentStorage : IObservable<OpenApiDocumentStorageEventArgs>
{
    ValueTask<IEnumerable<OpenApiDocumentDefinition>> GetDocumentsAsync(CancellationToken cancellationToken = default);
}
