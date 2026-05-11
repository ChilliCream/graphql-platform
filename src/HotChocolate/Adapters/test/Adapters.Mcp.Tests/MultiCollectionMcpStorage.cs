using HotChocolate.Adapters.Mcp.Storage;

namespace HotChocolate.Adapters.Mcp;

/// <summary>
/// Test storage that returns the supplied definitions verbatim, allowing duplicate
/// names. This simulates a production scenario where multiple collections are
/// published to the same stage and surface overlapping definitions.
/// </summary>
internal sealed class MultiCollectionMcpStorage : IMcpStorage
{
    private readonly IReadOnlyList<PromptDefinition> _prompts;
    private readonly IReadOnlyList<OperationToolDefinition> _tools;

    public MultiCollectionMcpStorage(
        IReadOnlyList<PromptDefinition>? prompts = null,
        IReadOnlyList<OperationToolDefinition>? tools = null)
    {
        _prompts = prompts ?? [];
        _tools = tools ?? [];
    }

    public ValueTask<IEnumerable<OperationToolDefinition>> GetOperationToolDefinitionsAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IEnumerable<OperationToolDefinition>>(_tools);

    public ValueTask<IEnumerable<PromptDefinition>> GetPromptDefinitionsAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IEnumerable<PromptDefinition>>(_prompts);

    public IDisposable Subscribe(IObserver<OperationToolStorageEventArgs> observer)
        => NoOpSubscription.Instance;

    public IDisposable Subscribe(IObserver<PromptStorageEventArgs> observer)
        => NoOpSubscription.Instance;

    private sealed class NoOpSubscription : IDisposable
    {
        public static readonly NoOpSubscription Instance = new();

        public void Dispose()
        {
        }
    }
}
