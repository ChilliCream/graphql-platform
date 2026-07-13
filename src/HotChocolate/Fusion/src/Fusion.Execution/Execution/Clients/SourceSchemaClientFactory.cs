using HotChocolate.Fusion.Types;

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
    public ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        ISourceSchemaClientConfiguration configuration)
    {
        if (configuration is not TConfiguration casted)
        {
            throw ThrowHelper.InvalidClientConfiguration(typeof(TConfiguration), configuration.GetType());
        }

        return CreateClient(schema, casted);
    }

    /// <summary>
    /// Creates a new <see cref="ISourceSchemaClient"/> for the given typed configuration.
    /// </summary>
    /// <param name="schema">The composed Fusion gateway schema.</param>
    /// <param name="configuration">The typed client configuration.</param>
    /// <returns>A new source schema client instance.</returns>
    protected abstract ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        TConfiguration configuration);
}
