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

    /// <summary>
    /// The parameter that names the declared stage an invocation publishes to.
    /// </summary>
    internal ParameterResource? StageParameter { get; set; }

    internal string? ConfigurationTag { get; set; }

    internal ParameterResource? ConfigurationTagParameter { get; set; }
}
