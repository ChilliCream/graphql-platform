using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Represents the Fusion gateway configuration.
/// </summary>
public sealed class GatewayConfiguration
{
    public GatewayConfiguration(DocumentNode document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }

    /// <summary>
    /// Gets the Fusion gateway configuration document.
    /// </summary>
    public DocumentNode Document { get; }
}
