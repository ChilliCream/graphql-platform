using HotChocolate.Execution;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.InMemory;

/// <inheritdoc />
public class DefaultInMemoryClientFactory
    : IInMemoryClientFactory
{
    private readonly IRequestExecutorResolver _requestExecutorResolver;
    private readonly IOptionsMonitor<InMemoryClientFactoryOptions> _optionsMonitor;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultInMemoryClientFactory"/>
    /// </summary>
    /// <param name="requestExecutorResolver">
    /// The <see cref="RequestExecutorResolver"/> that should be used to resolve the schemas
    /// </param>
    /// <param name="optionsMonitor">
    /// The options monitor for the factory options
    /// </param>
    public DefaultInMemoryClientFactory(
        IRequestExecutorResolver requestExecutorResolver,
        IOptionsMonitor<InMemoryClientFactoryOptions> optionsMonitor)
    {
        _requestExecutorResolver = requestExecutorResolver ??
            throw new ArgumentNullException(nameof(optionsMonitor));
        _optionsMonitor = optionsMonitor ??
            throw new ArgumentNullException(nameof(optionsMonitor));
    }

    /// <inheritdoc />
    public async ValueTask<IInMemoryClient> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw ThrowHelper.Argument_IsNullOrEmpty(nameof(name));
        }

        var options = _optionsMonitor.Get(name);
        var client = new InMemoryClient(name);

        for (var i = 0; i < options.InMemoryClientActions.Count; i++)
        {
            await options.InMemoryClientActions[i](client, cancellationToken);
        }

        if (client.Executor is not null)
        {
            client.SchemaName = client.Executor.Schema.Name;
        }
        else
        {
            client.Executor = await _requestExecutorResolver
                .GetRequestExecutorAsync(client.SchemaName, cancellationToken);
        }

        return client;
    }
}
