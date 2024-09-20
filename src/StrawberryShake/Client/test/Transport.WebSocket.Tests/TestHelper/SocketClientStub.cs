using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace StrawberryShake.Transport.WebSockets;

public sealed class SocketClientStub : ISocketClient
{
    private readonly ConcurrentDictionary<MemberInfo, int> _callCount = new();
    private readonly TaskCompletionSource<bool> _completionSource =
        new(TaskCreationOptions.None);
    private bool _isClosed = true;

    public event EventHandler? OnConnectionClosed { add { } remove { } }

    public SemaphoreSlim Blocker { get; } = new(0);

    public Uri? Uri { get; set; }

    public ISocketConnectionInterceptor ConnectionInterceptor { get; set; } =
        DefaultSocketConnectionInterceptor.Instance;

    public string Name { get; set; } = default!;

    public Queue<string> MessagesReceive { get; } = new();

    public Queue<string> SentMessages { get; } = new();

    public ISocketProtocol? Protocol { get; set; }

    public SocketCloseStatus CloseStatus { get; set; }

    public string? CloseMessage { get; set; }

    public CancellationToken LatestCancellationToken { get; private set; } =
        CancellationToken.None;

    public bool IsClosed
    {
        get => _isClosed && !KeepOpen;
        set => _isClosed = value;
    }

    public bool KeepOpen { get; set; }

    public bool IsDisposed { get; set; }

    public ValueTask SendAsync(
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken = default)
    {
        Increment(x => x.SendAsync(default!, default!));

        SentMessages.Enqueue(Encoding.UTF8.GetString(message.Span));
        LatestCancellationToken = cancellationToken;
        return default;
    }

    public async ValueTask ReceiveAsync(
        PipeWriter writer,
        CancellationToken cancellationToken = default)
    {
        Increment(x => x.ReceiveAsync(default!, default!));

        LatestCancellationToken = cancellationToken;
        if (MessagesReceive.TryDequeue(out var message))
        {
            var messageAsByte = Encoding.UTF8.GetBytes(message);
            await writer.WriteAsync(new ReadOnlyMemory<byte>(messageAsByte), cancellationToken);
            await writer.FlushAsync(cancellationToken);
            return;
        }

        _completionSource.SetResult(true);

        await Blocker.WaitAsync(cancellationToken);
    }

    public Task<ISocketProtocol> OpenAsync(CancellationToken cancellationToken = default)
    {
        Increment(x => x.OpenAsync(default!));

        IsClosed = false;
        return Task.FromResult(Protocol!);
    }

    public Task CloseAsync(
        string message,
        SocketCloseStatus closeStatus,
        CancellationToken cancellationToken = default)
    {
        Increment(x => x.CloseAsync(default!, default!, default!));

        CloseMessage = message;
        CloseStatus = closeStatus;
        IsClosed = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }

    public Task WaitTillFinished()
    {
        return _completionSource.Task;
    }

    public void Increment(Expression<Action<ISocketClient>> member)
    {
        if (member.Body is MethodCallExpression { Method: { } m, })
        {
            _callCount.AddOrUpdate(m, _ => 1, (m, c) => c + 1);
        }
    }

    public int GetCallCount(Expression<Action<ISocketClient>> member)
    {
        if (member.Body is MethodCallExpression { Method: { } m, })
        {
            _callCount.TryGetValue(m, out var counter);
            return counter;
        }

        throw new InvalidOperationException();
    }
}
