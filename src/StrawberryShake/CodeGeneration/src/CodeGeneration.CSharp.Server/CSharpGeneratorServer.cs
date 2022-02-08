using System;
using System.IO;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp;

public sealed partial class CSharpGeneratorServer : IObserver<IMessage>, IAsyncDisposable
{
    private readonly TaskCompletionSource _tcs = new();
    private readonly MessageBus _messageBus;
    private readonly IDisposable _subscription;
    private bool _disposed;

    public CSharpGeneratorServer(Stream readStream, Stream writeStream)
    {
        _messageBus = new MessageBus(readStream, writeStream);
        _subscription = _messageBus.Subscribe(this);
    }

    public Task Completion => _tcs.Task;

    public void OnNext(IMessage value)
    {
        switch (value)
        {
            case GeneratorRequest request:
                Task.Run(async () => await ExecuteGenerateAsync(request));
                break;

            case CloseMessage:
                _tcs.TrySetResult();
                break;
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
        _tcs.TrySetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _subscription.Dispose();
            await _messageBus.DisposeAsync();
            _disposed = true;
        }
    }

    public static async Task RunAsync()
    {
        await using var server = new CSharpGeneratorServer(
            Console.OpenStandardInput(),
            Console.OpenStandardOutput());
        await server.Completion;
    }
}
