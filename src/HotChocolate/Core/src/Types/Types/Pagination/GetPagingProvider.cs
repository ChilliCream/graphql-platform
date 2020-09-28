using System;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public delegate IPagingProvider GetPagingProvider(
        IServiceProvider services,
        IExtendedType sourceType);
}
