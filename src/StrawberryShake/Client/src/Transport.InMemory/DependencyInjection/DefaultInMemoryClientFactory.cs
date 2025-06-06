using HotChocolate.Execution;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.InMemory;

/// <inheritdoc />
public class DefaultInMemoryClientFactory : IInMemoryClientFactory
{
    private readonly IRequestExecutorProvider _executorProvider;
    private readonly IOptionsMonitor<InMemoryClientFactoryOptions> _optionsMonitor;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultInMemoryClientFactory"/>
    /// </summary>
    /// <param name="executorProvider">
    /// The <see cref="IRequestExecutorProvider"/> that should be used to resolve the schemas
    /// </param>
    /// <param name="optionsMonitor">
    /// The options monitor for the factory options
    /// </param>
    public DefaultInMemoryClientFactory(
        IRequestExecutorProvider executorProvider,
        IOptionsMonitor<InMemoryClientFactoryOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(executorProvider);
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _executorProvider = executorProvider;
        _optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc />
    public async ValueTask<IInMemoryClient> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var options = _optionsMonitor.Get(name);
        var client = new InMemoryClient(name);

        foreach (var configurationAction in options.InMemoryClientActions)
        {
            await configurationAction(client, cancellationToken);
        }

        if (client.Executor is not null)
        {
            client.SchemaName = client.Executor.Schema.Name;
        }
        else
        {
            client.Executor = await _executorProvider.GetExecutorAsync(client.SchemaName, cancellationToken);
        }

        return client;
    }
}
