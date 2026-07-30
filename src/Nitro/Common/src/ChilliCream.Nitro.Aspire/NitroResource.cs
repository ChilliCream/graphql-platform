using Aspire.Hosting.ApplicationModel;

namespace ChilliCream.Nitro.Aspire;

/// <summary>
/// Represents a Nitro target in an Aspire application model.
/// </summary>
public sealed class NitroResource(string name) : Resource(name)
{
    internal string? CloudUrl { get; set; }

    internal string? ApiId { get; set; }

    internal ParameterResource? ApiKey { get; set; }
}
