using System;
using HotChocolate.Internal;

namespace HotChocolate.Types.Pagination
{
    public abstract class CursorPagingProvider : IPagingProvider
    {
        public abstract bool CanHandle(IExtendedType source);

        IPagingHandler IPagingProvider.CreateHandler(
            IExtendedType source,
            PagingSettings settings) =>
            CreateHandler(source, settings);

        protected abstract CursorPagingHandler CreateHandler(
            IExtendedType source,
            PagingSettings settings);
    }
}
