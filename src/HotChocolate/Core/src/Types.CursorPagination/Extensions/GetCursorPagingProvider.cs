using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

/// <summary>
/// A delegate to resolve the optimal paging provider
/// for the specified <paramref name="sourceType"/>.
/// </summary>
/// <param name="services">
/// The application services.
/// </param>
/// <param name="sourceType">
/// The source type.
/// </param>
/// <param name="providerName">
/// The paging provider name that shall be selected.
/// </param>
/// <returns>
/// Returns a paging provider for the specified <paramref name="sourceType"/>.
/// </returns>
public delegate CursorPagingProvider GetCursorPagingProvider(
    IServiceProvider services,
    IExtendedType sourceType,
    string? providerName = null);
