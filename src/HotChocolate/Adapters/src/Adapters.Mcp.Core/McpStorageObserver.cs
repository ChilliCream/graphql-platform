using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using HotChocolate.Adapters.Mcp.Diagnostics;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using static ModelContextProtocol.Protocol.NotificationMethods;

namespace HotChocolate.Adapters.Mcp;

internal sealed class McpStorageObserver : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(initialCount: 1, maxCount: 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _ct;
    private readonly ISchemaDefinition _schema;
    private readonly McpFeatureRegistry _registry;
    private readonly OperationToolFactory _toolFactory;
    private readonly ConcurrentDictionary<string, McpServer> _mcpServers;
    private readonly IMcpStorage _storage;
    private readonly IMcpDiagnosticEvents _diagnosticEvents;
    private IDisposable? _promptsSubscription;
    private IDisposable? _toolsSubscription;
#if NET10_0_OR_GREATER
    private ImmutableDictionary<string, (Prompt, ImmutableArray<PromptMessage>)> _prompts = [];
    private ImmutableDictionary<string, OperationTool> _tools = [];
#else
    private ImmutableDictionary<string, (Prompt, ImmutableArray<PromptMessage>)> _prompts
        = ImmutableDictionary<string, (Prompt, ImmutableArray<PromptMessage>)>.Empty;
    private ImmutableDictionary<string, OperationTool> _tools = ImmutableDictionary<string, OperationTool>.Empty;
#endif
    private static readonly DocumentValidator s_documentValidator
        = DocumentValidatorBuilder.New().AddDefaultRules().Build();
    private bool _disposed;

    public McpStorageObserver(
        ISchemaDefinition schema,
        McpFeatureRegistry registry,
        OperationToolFactory toolFactory,
        ConcurrentDictionary<string, McpServer> mcpServers,
        IMcpStorage storage,
        IMcpDiagnosticEvents diagnosticEvents)
    {
        _schema = schema;
        _registry = registry;
        _toolFactory = toolFactory;
        _mcpServers = mcpServers;
        _storage = storage;
        _diagnosticEvents = diagnosticEvents;
        _ct = _cts.Token;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_promptsSubscription is not null || _toolsSubscription is not null)
        {
            return;
        }

        await _semaphore.WaitAsync(cancellationToken);

        _promptsSubscription = _storage
            .Buffer<PromptStorageEventArgs>(TimeSpan.FromMilliseconds(500), 10)
            .Where(batch => batch.Count > 0)
            .Subscribe(onNext: ProcessBatch);

        _toolsSubscription = _storage
            .Buffer<OperationToolStorageEventArgs>(TimeSpan.FromMilliseconds(500), 10)
            .Where(batch => batch.Count > 0)
            .Subscribe(onNext: ProcessBatch);

        try
        {
            await Task.WhenAll(
                InitializePromptsAsync(cancellationToken),
                InitializeToolsAsync(cancellationToken));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task InitializePromptsAsync(CancellationToken cancellationToken)
    {
        var prompts = ImmutableDictionary.CreateBuilder<string, (Prompt, ImmutableArray<PromptMessage>)>();
        using var scope = _diagnosticEvents.InitializePrompts();

        foreach (var promptDefinition in await _storage.GetPromptDefinitionsAsync(cancellationToken))
        {
            var prompt = PromptFactory.CreatePrompt(promptDefinition);
            prompts.Add(promptDefinition.Name, prompt);
        }

        _prompts = prompts.ToImmutable();
        _registry.UpdatePrompts(_prompts);
    }

    private async Task InitializeToolsAsync(CancellationToken cancellationToken)
    {
        var tools = ImmutableDictionary.CreateBuilder<string, OperationTool>();
        using var scope = _diagnosticEvents.InitializeTools();

        foreach (var toolDefinition in await _storage.GetOperationToolDefinitionsAsync(cancellationToken))
        {
            var validationResult = s_documentValidator.Validate(_schema, toolDefinition.Document);

            if (validationResult.HasErrors)
            {
                _diagnosticEvents.ValidationErrors(validationResult.Errors);
                continue;
            }

            tools.Add(toolDefinition.Name, _toolFactory.CreateTool(toolDefinition));
        }

        _tools = tools.ToImmutable();
        _registry.UpdateTools(_tools);
    }

    private void ProcessBatch(IList<PromptStorageEventArgs> eventArgs)
    {
        _semaphore.Wait(_ct);

        try
        {
            foreach (var eventArg in eventArgs)
            {
                switch (eventArg.Type)
                {
                    case PromptStorageEventType.Added:
                    case PromptStorageEventType.Modified:
                        using (_diagnosticEvents.UpdatePrompts())
                        {
                            var prompt = PromptFactory.CreatePrompt(eventArg.PromptDefinition!);
                            _prompts = _prompts.SetItem(eventArg.Name, prompt);
                            break;
                        }

                    case PromptStorageEventType.Removed:
                        _prompts = _prompts.Remove(eventArg.Name);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _registry.UpdatePrompts(_prompts);
        }
        finally
        {
            _semaphore.Release();
        }

        foreach (var mcpServer in _mcpServers.Values)
        {
            mcpServer.SendNotificationAsync(PromptListChangedNotification, cancellationToken: _ct).FireAndForget();
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
                        using (_diagnosticEvents.UpdateTools())
                        {
                            var validationResult =
                                s_documentValidator.Validate(_schema, eventArg.ToolDefinition!.Document);

                            if (validationResult.HasErrors)
                            {
                                _diagnosticEvents.ValidationErrors(validationResult.Errors);
                                continue;
                            }

                            var tool = _toolFactory.CreateTool(eventArg.ToolDefinition!);
                            _tools = _tools.SetItem(eventArg.Name, tool);
                            break;
                        }

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

        foreach (var mcpServer in _mcpServers.Values)
        {
            mcpServer.SendNotificationAsync(ToolListChangedNotification, cancellationToken: _ct).FireAndForget();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _promptsSubscription?.Dispose();
        _toolsSubscription?.Dispose();
        _cts.Cancel();
        _cts.Dispose();
        _semaphore.Dispose();
        _promptsSubscription = null;
        _toolsSubscription = null;
    }
}
