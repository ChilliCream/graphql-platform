using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A delegate that is used to resolve a fusion graph document.
/// </summary>
/// <param name="services">
/// The application services.
/// </param>
/// <param name="cancellationToken">
/// The cancellation token.
/// </param>
public delegate ValueTask<DocumentNode> ResolveFusionGraphDocServiceProviderAsync(
    IServiceProvider services,
    CancellationToken cancellationToken = default);
