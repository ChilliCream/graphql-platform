using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// This interface allows to rewrite the gateway configuration before it is applied.
/// </summary>
public interface IConfigurationRewriter
{
    /// <summary>
    /// Rewrites the gateway configuration.
    /// </summary>
    /// <param name="configuration">
    /// The gateway configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the rewritten gateway configuration.
    /// </returns>
    ValueTask<DocumentNode> RewriteAsync(
        DocumentNode configuration,
        CancellationToken cancellationToken = default);
}
