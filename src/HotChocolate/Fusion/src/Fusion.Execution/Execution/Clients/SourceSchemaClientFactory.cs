namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Base class for <see cref="ISourceSchemaClientFactory"/> implementations that are
/// bound to a specific <see cref="ISourceSchemaClientConfiguration"/> type.
/// Handles the type check in <see cref="CanHandle"/> and the cast in
/// <see cref="ISourceSchemaClientFactory.CreateClient"/> automatically.
/// </summary>
/// <typeparam name="TConfiguration">
/// The configuration type this factory handles.
/// </typeparam>
public abstract class SourceSchemaClientFactory<TConfiguration> : ISourceSchemaClientFactory
    where TConfiguration : ISourceSchemaClientConfiguration
{
    /// <inheritdoc />
    public bool CanHandle(ISourceSchemaClientConfiguration configuration)
        => configuration is TConfiguration;

    /// <inheritdoc />
    ISourceSchemaClient ISourceSchemaClientFactory.CreateClient(ISourceSchemaClientConfiguration configuration)
        => CreateClient((TConfiguration)configuration);

    /// <summary>
    /// Creates a new <see cref="ISourceSchemaClient"/> for the given typed configuration.
    /// </summary>
    /// <param name="configuration">The typed client configuration.</param>
    /// <returns>A new source schema client instance.</returns>
    protected abstract ISourceSchemaClient CreateClient(TConfiguration configuration);
}
