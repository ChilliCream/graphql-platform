using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp;

public class CSharpGeneratorClient : IAsyncDisposable, IObserver<IMessage>
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
        var tcs = _tcs = new TaskCompletionSource<GeneratorResponse>();
        await _messageBus.SendAsync(request, cancellationToken);
        cancellationToken.Register(() => tcs.TrySetCanceled());
        return await _tcs.Task;
    }

    public void OnNext(IMessage value)
    {
        if (value is GeneratorResponse response)
        {
            _tcs?.TrySetResult(response);
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

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
}
