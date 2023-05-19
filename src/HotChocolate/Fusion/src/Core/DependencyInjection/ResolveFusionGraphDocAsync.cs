using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A delegate that is used to resolve a fusion graph document.
/// </summary>
/// <param name="cancellationToken">
/// The cancellation token.
/// </param>
public delegate ValueTask<DocumentNode> ResolveFusionGraphDocAsync(
    CancellationToken cancellationToken = default);
