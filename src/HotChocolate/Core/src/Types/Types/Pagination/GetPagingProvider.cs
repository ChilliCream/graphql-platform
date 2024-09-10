using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Types.Pagination;

/// <summary>
/// A delegate to resolve a paging provider.
/// </summary>
/// <param name="services">The service provider to resolver the paging providers from.</param>
/// <param name="sourceType">The type that is returned by the resolver.</param>
/// <param name="providerName">The name of the provider that shall be selected.</param>
public delegate IPagingProvider GetPagingProvider(
    IServiceProvider services,
    IExtendedType sourceType,
    string? providerName);
