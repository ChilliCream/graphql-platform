using System.Buffers;
using System.Reactive.Disposables;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Configuration;

public sealed class InMemoryFusionConfigurationProvider : IFusionConfigurationProvider
{
    private readonly JsonDocumentOwner? _schemaSettings;

    public InMemoryFusionConfigurationProvider(DocumentNode schemaDocument, JsonDocumentOwner? schemaSettings)
    {
        ArgumentNullException.ThrowIfNull(schemaDocument);

        Configuration = new FusionConfiguration(
            schemaDocument,
            new JsonDocumentOwner(
                schemaSettings?.Document ?? JsonDocument.Parse("{ }"),
                EmptyMemoryOwner.Instance));
        _schemaSettings = schemaSettings;
    }

    public FusionConfiguration Configuration { get; }

    public IDisposable Subscribe(IObserver<FusionConfiguration> observer)
    {
        observer.OnNext(Configuration);
        observer.OnCompleted();
        return Disposable.Empty;
    }

    public ValueTask DisposeAsync()
    {
        _schemaSettings?.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class EmptyMemoryOwner : IMemoryOwner<byte>
    {
        public static readonly EmptyMemoryOwner Instance = new();

        public Memory<byte> Memory => default;

        public void Dispose()
        {
        }
    }
}
