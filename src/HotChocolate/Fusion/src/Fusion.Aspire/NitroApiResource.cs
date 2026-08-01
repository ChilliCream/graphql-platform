using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Represents an API declared in Nitro.
/// </summary>
public sealed class NitroApiResource(
    string name,
    string apiName,
    NitroResource nitro)
    : Resource(name)
{
    internal NitroResource Nitro { get; } = nitro;

    /// <summary>
    /// Gets the declared name of the Nitro API.
    /// </summary>
    public string ApiName { get; } = apiName;

    internal string? ApiId { get; set; }
}
