using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public interface IPagingProvider
    {
        bool CanHandle(IExtendedType source);

        IPagingHandler CreateHandler(PagingSettings settings);
    }
}
