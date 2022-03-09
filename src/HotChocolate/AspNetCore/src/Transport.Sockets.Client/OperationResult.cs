using System;
using System.Text.Json;

namespace HotChocolate.Transport.Sockets.Client;

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

