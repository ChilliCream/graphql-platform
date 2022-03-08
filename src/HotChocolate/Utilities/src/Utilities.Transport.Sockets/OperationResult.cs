using System.Text.Json;
using static HotChocolate.Utilities.Transport.Sockets.OperationResultProperties;

namespace HotChocolate.Utilities.Transport.Sockets;

public sealed class OperationResult : IDisposable
{
    private readonly JsonDocument _document;
    private bool _disposed;

    public OperationResult(JsonDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));

        if (document.RootElement.TryGetProperty(DataProp, out JsonElement dataProp))
        {
            Data = dataProp;
        }

        if (document.RootElement.TryGetProperty(ErrorsProp, out JsonElement errorsProp))
        {
            Errors = errorsProp;
        }

        if (document.RootElement.TryGetProperty(ExtensionsProp, out JsonElement extensionsProp))
        {
            Extensions = extensionsProp;
        }
    }

    public JsonElement? Data { get; }

    public JsonElement? Errors { get; }

    public JsonElement? Extensions { get; }

    public void Dispose()
    {
        if (!_disposed)
        {
            _document.Dispose();
            _disposed = true;
        }
    }
}
