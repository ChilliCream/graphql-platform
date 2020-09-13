using System;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types
{
    public delegate CursorPagingProvider GetCursorPagingProvider(
        IServiceProvider services,
        IExtendedType sourceType);
}
