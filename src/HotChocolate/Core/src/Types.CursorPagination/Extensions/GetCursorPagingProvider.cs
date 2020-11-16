using System;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types
{
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
    /// <returns>
    /// Returns a paging provider for the specified <paramref name="sourceType"/>.
    /// </returns>
    public delegate CursorPagingProvider GetCursorPagingProvider(
        IServiceProvider services,
        IExtendedType sourceType);
}
