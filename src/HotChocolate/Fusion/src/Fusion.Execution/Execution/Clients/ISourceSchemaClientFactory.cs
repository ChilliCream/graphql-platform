using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// A factory that creates <see cref="ISourceSchemaClient"/> instances
/// from a matching <see cref="ISourceSchemaClientConfiguration"/>.
/// Each connector type (HTTP, in-memory, etc.) provides its own factory implementation.
/// </summary>
public interface ISourceSchemaClientFactory
{
    /// <summary>
    /// Determines whether this factory can create a client for the given configuration.
    /// </summary>
    /// <param name="configuration">The client configuration to check.</param>
    /// <returns><c>true</c> if this factory handles the given configuration type.</returns>
    bool CanHandle(ISourceSchemaClientConfiguration configuration);

    /// <summary>
    /// Creates a new <see cref="ISourceSchemaClient"/> for the given configuration.
    /// </summary>
    /// <param name="schema">The composed Fusion gateway schema.</param>
    /// <param name="configuration">The client configuration.</param>
    /// <returns>A new source schema client instance.</returns>
    ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        ISourceSchemaClientConfiguration configuration);
}
