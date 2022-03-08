using System.Text.Json;
using static HotChocolate.Utilities.Transport.Sockets.Helpers.OperationResultProperties;

namespace HotChocolate.Utilities.Transport.Sockets;

public sealed class OperationResult : IDisposable
{
    private readonly JsonDocument _document;
    private bool _disposed;

    public OperationResult(
        JsonDocument document,
        JsonElement? data = null,
        JsonElement? errors = null,
        JsonElement? extensions = null)
    {
        _document = document;
        Data = data;
        Errors = errors;
        Extensions = extensions;
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
