using System;
using HotChocolate.Internal;

namespace HotChocolate.Types.Pagination
{
    public abstract class CursorPagingProvider : IPagingProvider
    {
        public abstract bool CanHandle(IExtendedType source);

        IPagingHandler IPagingProvider.CreateHandler(
            IExtendedType source,
            PagingOptions options) =>
            CreateHandler(source, options);

        protected abstract CursorPagingHandler CreateHandler(
            IExtendedType source,
            PagingOptions options);
    }
}
