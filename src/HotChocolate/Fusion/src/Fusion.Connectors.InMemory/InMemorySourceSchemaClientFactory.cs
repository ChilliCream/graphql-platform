using HotChocolate.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Transport.Formatters;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// A factory that creates <see cref="InMemorySourceSchemaClient"/> instances
/// for source schemas configured with <see cref="InMemorySourceSchemaClientConfiguration"/>.
/// </summary>
public sealed class InMemorySourceSchemaClientFactory
    : SourceSchemaClientFactory<InMemorySourceSchemaClientConfiguration>
{
    private readonly IRequestExecutorProvider _executorProvider;
    private readonly IRequestExecutorEvents _executorEvents;
    private readonly JsonResultFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemorySourceSchemaClientFactory"/>.
    /// </summary>
    /// <param name="executorProvider">The provider for resolving request executors.</param>
    /// <param name="executorEvents">The event source for executor lifecycle events.</param>
    /// <param name="formatter">The JSON result formatter.</param>
    public InMemorySourceSchemaClientFactory(
        IRequestExecutorProvider executorProvider,
        IRequestExecutorEvents executorEvents,
        JsonResultFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(executorProvider);
        ArgumentNullException.ThrowIfNull(executorEvents);
        ArgumentNullException.ThrowIfNull(formatter);

        _executorProvider = executorProvider;
        _executorEvents = executorEvents;
        _formatter = formatter;
    }

    /// <inheritdoc />
    protected override ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        InMemorySourceSchemaClientConfiguration configuration)
    {
        var proxy = new RequestExecutorProxy(_executorProvider, _executorEvents, configuration.Name);
        return new InMemorySourceSchemaClient(proxy, _formatter);
    }
}
