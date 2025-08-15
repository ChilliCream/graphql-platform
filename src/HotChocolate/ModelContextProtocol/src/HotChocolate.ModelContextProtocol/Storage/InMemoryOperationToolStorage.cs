using CaseConverter;
using HotChocolate.Language;

namespace HotChocolate.ModelContextProtocol.Storage;

/// <summary>
/// In-memory implementation of <see cref="IOperationToolStorage"/> for testing purposes only.
/// Provides thread-safe storage with synchronous change notifications.
/// </summary>
public sealed class InMemoryOperationToolStorage : OperationToolStorageBase
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, OperationToolDefinition> _tools = [];

    /// <inheritdoc />
    public override async ValueTask<IEnumerable<OperationToolDefinition>> GetToolsAsync(
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return _tools.Values.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Adds or updates a tool using the operation name as the tool identifier.
    /// </summary>
    /// <param name="document">GraphQL document containing exactly one named operation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when document has no operation definition or operation is unnamed.</exception>
    public Task AddOrUpdateToolAsync(
        DocumentNode document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var operation = document.Definitions.OfType<OperationDefinitionNode>().FirstOrDefault();

        if (operation is null)
        {
            throw new ArgumentException($"Document {document} has no operation definition");
        }

        var name = operation.Name?.Value.ToSnakeCase();
        return AddOrUpdateToolAsync(name!, document, cancellationToken);
    }

    /// <summary>
    /// Adds or updates a tool with the specified name.
    /// Fires <see cref="OperationToolStorageEventType.Added"/> for new tools or
    /// <see cref="OperationToolStorageEventType.Modified"/> for existing tools.
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    /// <param name="document">GraphQL document containing the operation definition.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddOrUpdateToolAsync(
        string name,
        DocumentNode document,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(document);

        OperationToolStorageEventType type;
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            var tool = new OperationToolDefinition(name, document);
            if (_tools.TryAdd(name, tool))
            {
                type = OperationToolStorageEventType.Added;
            }
            else
            {
                _tools[name] = tool;
                type = OperationToolStorageEventType.Modified;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        NotifySubscribers(name, document, type);
    }

    /// <summary>
    /// Removes a tool from storage.
    /// Fires <see cref="OperationToolStorageEventType.Removed"/> if the tool existed.
    /// </summary>
    /// <param name="name">The name of the tool to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveToolAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        await _semaphore.WaitAsync(cancellationToken);
        bool removed;

        try
        {
            removed = _tools.Remove(name);
        }
        finally
        {
            _semaphore.Release();
        }

        if (removed)
        {
            NotifySubscribers(name, null, OperationToolStorageEventType.Removed);
        }
    }
}
