using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// A source schema client factory that hands out one client per configuration to all
/// requests until the client expires or is invalidated.
/// </summary>
internal interface ISharedSourceSchemaClientFactory
{
    /// <summary>
    /// Gets the client that all requests share for the given configuration.
    /// </summary>
    /// <param name="schema">The schema the client is used for.</param>
    /// <param name="configuration">The client configuration.</param>
    /// <param name="client">The shared client.</param>
    /// <returns>
    /// <c>false</c> if clients of the given configuration are not shared.
    /// </returns>
    bool TryGetSharedClient(
        FusionSchemaDefinition schema,
        ISourceSchemaClientConfiguration configuration,
        [NotNullWhen(true)] out SharedSourceSchemaClient? client);
}
