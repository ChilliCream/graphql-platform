using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Represents the Fusion gateway configuration.
/// </summary>
public sealed class GatewayConfiguration(DocumentNode document)
{
    /// <summary>
    /// Gets the Fusion gateway configuration document.
    /// </summary>
    public DocumentNode Document { get; } = document
        ?? throw new ArgumentNullException(nameof(document));
}
