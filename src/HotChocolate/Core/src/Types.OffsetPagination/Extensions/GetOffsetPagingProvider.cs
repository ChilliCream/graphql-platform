using System;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types
{
    public delegate OffsetPagingProvider GetOffsetPagingProvider(
        IServiceProvider services,
        IExtendedType sourceType);
}
