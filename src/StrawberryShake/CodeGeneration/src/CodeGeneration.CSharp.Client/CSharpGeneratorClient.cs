using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp;

public class CSharpGeneratorClient
#if NETSTANDARD2_0
    : IDisposable
#else
    : IAsyncDisposable
#endif
    , IObserver<IMessage>
{
    private readonly MessageBus _messageBus;
    private readonly IDisposable _subscription;
    private TaskCompletionSource<GeneratorResponse>? _tcs;
    private bool _disposed;

    public CSharpGeneratorClient(Stream readStream, Stream writeStream)
    {
        if (readStream is null)
        {
            throw new ArgumentNullException(nameof(readStream));
        }

        if (writeStream is null)
        {
            throw new ArgumentNullException(nameof(writeStream));
        }

        _messageBus = new MessageBus(readStream, writeStream);
        _subscription = _messageBus.Subscribe(this);
    }

    public async Task<GeneratorResponse> GenerateAsync(
        GeneratorRequest request,
        CancellationToken cancellationToken = default)
    {
        DebugLog.Log("GenerateAsync");
        var tcs = _tcs = new TaskCompletionSource<GeneratorResponse>();
        await _messageBus.SendAsync(request, cancellationToken);
        cancellationToken.Register(() => tcs.TrySetCanceled());
        var result =  await _tcs.Task;
        DebugLog.Log("GenerateAsync->result");
        return result;
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        DebugLog.Log("CloseAsync");
        await _messageBus.SendAsync(new CloseMessage(), cancellationToken);
        DebugLog.Log("CloseAsync->sent");
    }

    public void OnNext(IMessage value)
    {
        if (value is GeneratorResponse response)
        {
            DebugLog.Log("OnNext->response");
            _tcs?.TrySetResult(response);
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

#if NETSTANDARD2_0
    public void Dispose()
    {
        if (!_disposed)
        {
            DebugLog.Log("OnNext->dispose");
            _tcs?.TrySetCanceled();
            _subscription.Dispose();
            _messageBus.Dispose();
            _disposed = true;
            DebugLog.Log("OnNext->disposed");
        }
    }
#else
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _tcs?.TrySetCanceled();
            _subscription.Dispose();
            await _messageBus.DisposeAsync();
            _disposed = true;
        }
    }
#endif
}
