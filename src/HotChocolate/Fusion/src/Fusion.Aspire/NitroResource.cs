using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Represents the Nitro instance used by a distributed application.
/// </summary>
public sealed class NitroResource(string name) : Resource(name)
{
    internal string? CloudUrl { get; set; }

    internal ParameterResource? ApiKey { get; set; }

    internal Uri? PortalUrl { get; set; }

    internal NitroSeedUpdateOptions SeedUpdates { get; } = new();
}
