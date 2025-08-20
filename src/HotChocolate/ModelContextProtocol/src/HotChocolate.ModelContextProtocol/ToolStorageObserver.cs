using System.Collections.Immutable;
using System.Reactive.Linq;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.Utilities;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;
using static ModelContextProtocol.Protocol.NotificationMethods;

namespace HotChocolate.ModelContextProtocol;

internal sealed class ToolStorageObserver : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _ct;
    private readonly ToolRegistry _registry;
    private readonly OperationToolFactory _toolFactory;
    private readonly StreamableHttpHandler _httpHandler;
    private readonly IOperationToolStorage _storage;
    private IDisposable? _subscription;
#if NET10_0_OR_GREATER
    private ImmutableDictionary<string, OperationTool> _tools = [];
#else
    private ImmutableDictionary<string, OperationTool> _tools = ImmutableDictionary<string, OperationTool>.Empty;
#endif
    private bool _disposed;

    public ToolStorageObserver(
        ToolRegistry registry,
        OperationToolFactory toolFactory,
        StreamableHttpHandler httpHandler,
        IOperationToolStorage storage)
    {
        _registry = registry;
        _toolFactory = toolFactory;
        _storage = storage;
        _httpHandler = httpHandler;
        _ct = _cts.Token;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_subscription is not null)
        {
            return;
        }

        await _semaphore.WaitAsync(cancellationToken);

        _subscription = _storage
            .Buffer(TimeSpan.FromMilliseconds(500), 10)
            .Where(batch => batch.Count > 0)
            .Subscribe(onNext: ProcessBatch);

        try
        {
            var tools = ImmutableDictionary.CreateBuilder<string, OperationTool>();

            foreach (var tool in await _storage.GetToolsAsync(cancellationToken))
            {
                tools.Add(tool.Name, _toolFactory.CreateTool(tool.Name, tool.Document));
            }

            _tools = tools.ToImmutable();
            _registry.UpdateTools(_tools);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void ProcessBatch(IList<OperationToolStorageEventArgs> eventArgs)
    {
        _semaphore.Wait(_ct);

        try
        {
            foreach (var eventArg in eventArgs)
            {
                switch (eventArg.Type)
                {
                    case OperationToolStorageEventType.Added:
                    case OperationToolStorageEventType.Modified:
                        var tool = _toolFactory.CreateTool(eventArg.Name, eventArg.Document!);
                        _tools = _tools.SetItem(eventArg.Name, tool);
                        break;

                    case OperationToolStorageEventType.Removed:
                        _tools = _tools.Remove(eventArg.Name);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _registry.UpdateTools(_tools);
        }
        finally
        {
            _semaphore.Release();
        }

        foreach (var session in _httpHandler.Sessions.Values)
        {
            session?.Server?.SendNotificationAsync(ToolListChangedNotification, cancellationToken: _ct).FireAndForget();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _subscription?.Dispose();
        _cts.Cancel();
        _cts.Dispose();
        _semaphore.Dispose();
        _subscription = null;
    }
}
