using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Represents the Nitro api that the Fusion deployments of a distributed application publish to.
/// </summary>
public sealed class NitroPublishTargetResource(string name) : Resource(name)
{
    internal string? CloudUrl { get; set; }

    internal string? ApiId => this.GetNitroApiId();

    internal ParameterResource? ApiKey { get; set; }
}
