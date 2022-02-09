#if NETSTANDARD2_0
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
#else
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
#endif

namespace StrawberryShake.CodeGeneration.CSharp;

public class MessageBus
#if NETSTANDARD2_0
    : IDisposable
#else
    : IAsyncDisposable
#endif
    , IObservable<IMessage>
{
    private const byte _endMessage = 3;
    private const int _chunkSize = 128;
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private readonly List<Subscription> _observers = new();
    private readonly Stream _readStream;
    private readonly Stream _writeStream;
    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _abortProcessing;
    private bool _disposed;

    public MessageBus(Stream readStream, Stream writeStream)
    {
        _readStream = readStream ?? throw new ArgumentNullException(nameof(readStream));
        _writeStream = writeStream ?? throw new ArgumentNullException(nameof(writeStream));
        _cts = new CancellationTokenSource();
        _abortProcessing = _cts.Token;

        DebugLog.Log("Start->ProcessIncomingMessages");
        Task.Factory.StartNew(
            ProcessIncomingMessages,
            default,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public IDisposable Subscribe(IObserver<IMessage> observer)
    {
        if (observer is null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        var subscription = new Subscription(observer, _observers.Remove);
        _observers.Add(subscription);
        return subscription;
    }

    public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : IMessage
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var chunk = new byte[_chunkSize];
        var messageBuffer = JsonSerializer.SerializeToUtf8Bytes(message, _options);
        var index = 0;

        while (index < messageBuffer.Length)
        {
            int i;
            for (i = 0; i < _chunkSize; i++)
            {
                if (index < messageBuffer.Length)
                {
                    chunk[i] = messageBuffer[index++];
                }
                else
                {
                    break;
                }
            }

            if (index == messageBuffer.Length)
            {
                if (i < _chunkSize)
                {
                    chunk[i] = _endMessage;
                    await WriteChunkAsync();
                }
                else
                {
                    await WriteChunkAsync();

                    chunk[0] = _endMessage;
                    await WriteChunkAsync();
                }
            }
            else
            {
                await WriteChunkAsync();
            }
        }

        async Task WriteChunkAsync()
        {
            await _writeStream.WriteAsync(chunk, 0, chunk.Length, cancellationToken);
            Array.Clear(chunk, 0, chunk.Length);
        }
    }

#if NETSTANDARD2_0
    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Cancel();

            _readStream.Dispose();
            _writeStream.Dispose();

            _observers.ForEach(o => o.Dispose());
            _cts.Dispose();
            _disposed = true;
        }
    }
#else
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _cts.Cancel();

            await _readStream.DisposeAsync();
            await _writeStream.DisposeAsync();

            _observers.ForEach(o => o.Dispose());
            _cts.Dispose();
            _disposed = true;
        }
    }
#endif

    private async Task ProcessIncomingMessages()
    {
        var messageBufferSize = 1024;
        var buffer = new byte[_chunkSize];
        var messageBuffer = ArrayPool<byte>.Shared.Rent(messageBufferSize);

        try
        {
            while (!_abortProcessing.IsCancellationRequested)
            {
                var completed = false;
                var size = 0;

                do
                {
                    var read = await _readStream.ReadAsync(buffer, 0, _chunkSize, _abortProcessing);

                    for (var i = 0; i < read; i++)
                    {
                        var code = buffer[i];

                        if (code == _endMessage)
                        {
                            completed = true;
                            break;
                        }

                        messageBuffer[size++] = code;

                        if (messageBufferSize == size)
                        {
                            messageBufferSize *= 2;
                            var temp = messageBuffer;
                            messageBuffer = ArrayPool<byte>.Shared.Rent(messageBufferSize);
                            temp.CopyTo(messageBuffer, 0);
                            ArrayPool<byte>.Shared.Return(temp);
                        }
                    }

                    if (read == 0)
                    {
                        await Task.Delay(1000, _abortProcessing);
                    }
                }
                while (!completed);

                if (size > 0)
                {
                    DebugLog.Log("ProcessIncomingMessages->message");
                    IMessage message = Deserialize(messageBuffer, size);
                    _observers.ForEach(o => o.OnNext(message));
                    DebugLog.Log("ProcessIncomingMessages->message:" + message.Kind);

                    if (message.Kind == MessageKind.Close)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("ProcessIncomingMessages->error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            _observers.ForEach(o => o.OnCompleted());
        }
        finally
        {
            DebugLog.Log("ProcessIncomingMessages->done");
            _observers.ForEach(o => o.Dispose());
            ArrayPool<byte>.Shared.Return(messageBuffer);
        }
    }

    private IMessage Deserialize(byte[] buffer, int count)
    {
        var document = JsonDocument.Parse(buffer.AsMemory().Slice(0, count));

        if (document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("kind", out JsonElement kindProp) &&
            Enum.TryParse(kindProp.GetString(), out MessageKind kind))
        {
            switch (kind)
            {
                case MessageKind.Request:
#if NETSTANDARD2_0
                    return DeserializeGeneratorRequest(document.RootElement);
#else
                    return document.RootElement.Deserialize<GeneratorRequest>(_options)!;
#endif
                case MessageKind.Response:
#if NETSTANDARD2_0
                    return DeserializeGeneratorResponse(document.RootElement);
#else
                    return document.RootElement.Deserialize<GeneratorResponse>(_options)!;
#endif
                case MessageKind.Close:
                    return new CloseMessage();
            }
        }

        throw new InvalidDataException("Invalid Message.");
    }

#if NETSTANDARD2_0
    private static GeneratorRequest DeserializeGeneratorRequest(JsonElement root)
    {
        string? defaultNamespace = null;
        string? persistedQueryDirectory = null;

        if (root.TryGetProperty("defaultNamespace", out var prop))
        {
            defaultNamespace = prop.GetString();
        }

        if (root.TryGetProperty("persistedQueryDirectory", out prop))
        {
            persistedQueryDirectory = prop.GetString();
        }

        return new GeneratorRequest(
            root.GetProperty("configFileName").GetString(),
            root.GetProperty("documentFileNames")
                .EnumerateArray()
                .Select(t => t.GetString())
                .ToList(),
            defaultNamespace,
            persistedQueryDirectory);
    }

    private static GeneratorResponse DeserializeGeneratorResponse(JsonElement root)
    {
        var documents = new List<GeneratorDocument>();
        var errors = new List<GeneratorError>();

        if (root.TryGetProperty("documents", out var prop))
        {
            foreach (var doc in prop.EnumerateArray())
            {
                documents.Add(DeserializeGeneratorDocument(doc));
            }
        }

        if (root.TryGetProperty("errors", out prop))
        {
            foreach (var doc in prop.EnumerateArray())
            {
                errors.Add(DeserializeGeneratorError(doc));
            }
        }

        return new GeneratorResponse(documents, errors);
    }

    private static GeneratorDocument DeserializeGeneratorDocument(JsonElement root)
    {
        string? hash = null;
        string? path = null;

        if (root.TryGetProperty("hash", out var prop))
        {
            hash = prop.GetString();
        }

        if (root.TryGetProperty("path", out prop))
        {
            path = prop.GetString();
        }

        return new GeneratorDocument(
            root.GetProperty("name").GetString(),
            root.GetProperty("sourceText").GetString(),
            (GeneratorDocumentKind)Enum.Parse(
                typeof(GeneratorDocumentKind),
                root.GetProperty("kind").GetString()),
            hash,
            path);
    }

    private static GeneratorError DeserializeGeneratorError(JsonElement root)
    {
        string? filePath = null;
        Location? location = null;

        if (root.TryGetProperty("filePath", out var prop))
        {
            filePath = prop.GetString();
        }

        if (root.TryGetProperty("location", out prop))
        {
            location = DeserializeLocation(prop);
        }

        return new GeneratorError(
            root.GetProperty("code").GetString(),
            root.GetProperty("title").GetString(),
            root.GetProperty("message").GetString(),
            filePath,
            location);
    }

    private static Location DeserializeLocation(JsonElement root)
    {
        return new Location(
            root.GetProperty("line").GetInt32(),
            root.GetProperty("column").GetInt32());
    }
#endif

    private class Subscription : IDisposable, IObserver<IMessage>
    {
        private readonly IObserver<IMessage> _observer;
        private readonly Func<Subscription, bool> _unsubscribe;
        private bool _disposed;

        public Subscription(IObserver<IMessage> observer, Func<Subscription, bool> unsubscribe)
        {
            _observer = observer;
            _unsubscribe = unsubscribe;
        }

        public void OnNext(IMessage value) => _observer.OnNext(value);

        public void OnError(Exception error) => _observer.OnError(error);

        public void OnCompleted() => _observer.OnCompleted();

        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribe(this);
                _disposed = true;
            }
        }
    }
}
