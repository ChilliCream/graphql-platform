using System.Collections.Immutable;
using System.Reactive.Linq;
using HotChocolate.ModelContextProtocol.Factories;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.Utilities;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;
using static ModelContextProtocol.Protocol.NotificationMethods;

namespace HotChocolate.ModelContextProtocol.Registries;

internal sealed class GraphQLMcpToolStorageObserver : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _ct;
    private readonly GraphQLMcpToolRegistry _registry;
    private readonly GraphQLMcpToolFactory _toolFactory;
    private readonly StreamableHttpHandler _httpHandler;
    private readonly IMcpToolStorage _storage;
    private IDisposable? _subscription;
    private ImmutableDictionary<string, GraphQLMcpTool> _tools = ImmutableDictionary<string, GraphQLMcpTool>.Empty;
    private bool _disposed;

    public GraphQLMcpToolStorageObserver(
        GraphQLMcpToolRegistry registry,
        GraphQLMcpToolFactory toolFactory,
        StreamableHttpHandler httpHandler,
        IMcpToolStorage storage)
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
            var tools = ImmutableDictionary.CreateBuilder<string, GraphQLMcpTool>();

            await foreach (var tool in _storage.GetToolsAsync(cancellationToken))
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

    private void ProcessBatch(IList<McpToolStorageEventArgs> eventArgs)
    {
        _semaphore.Wait(_ct);

        try
        {
            foreach (var eventArg in eventArgs)
            {
                switch (eventArg.Type)
                {
                    case McpToolStorageEventType.Added:
                    case McpToolStorageEventType.Modified:
                        var tool = _toolFactory.CreateTool(eventArg.Name, eventArg.Document!);
                        _tools = _tools.SetItem(eventArg.Name, tool);
                        break;

                    case McpToolStorageEventType.Removed:
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

        var server = _httpHandler.Sessions.Values.FirstOrDefault()?.Server;
        server?.SendNotificationAsync(ToolListChangedNotification, cancellationToken: _ct).FireAndForget();
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
