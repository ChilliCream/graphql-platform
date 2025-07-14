using System.Reactive.Disposables;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Configuration;

public sealed class InMemoryFusionConfigurationProvider : IFusionSchemaDocumentProvider
{
    public InMemoryFusionConfigurationProvider(DocumentNode schemaDocument)
    {
        ArgumentNullException.ThrowIfNull(schemaDocument);

        SchemaDocument = schemaDocument;
    }

    public DocumentNode SchemaDocument { get; }

    public IDisposable Subscribe(IObserver<DocumentNode> observer)
    {
        observer.OnNext(SchemaDocument);
        observer.OnCompleted();
        return Disposable.Empty;
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}
